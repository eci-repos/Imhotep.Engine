using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.Governance.Models;
using Imhotep.Governance.Services;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;
using Imhotep.Planning.Models;
using Imhotep.Planning.Services;
using Imhotep.Repository.Models;
using Imhotep.Repository.Services;
using Imhotep.SemanticModel.Graph;
using Imhotep.Tools.Gateway;
using Imhotep.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Runtime.Services
{
   public interface IExecutionService
   {
      Task ExecuteConstructionWorkflowAsync(string transactionId, CanonicalSemanticModel activeModel, CancellationToken cancellationToken = default);
   }

   public class ExecutionService : IExecutionService
   {
      private readonly IPlanningEngine _planningEngine;
      private readonly IAgentOrchestrator _agentOrchestrator;
      private readonly IToolGateway _toolGateway;
      private readonly IGovernanceService _governanceService;
      private readonly IArtifactRepository _artifactRepository;
      private readonly ITelemetryService _telemetryService;
      private readonly ILogger<ExecutionService> _logger;

      private const int MaxRepairCycles = 5;

      public ExecutionService(
          IPlanningEngine planningEngine,
          IAgentOrchestrator agentOrchestrator,
          IToolGateway toolGateway,
          IGovernanceService governanceService,
          IArtifactRepository artifactRepository,
          ITelemetryService telemetryService,
          ILogger<ExecutionService> logger)
      {
         _planningEngine = planningEngine;
         _agentOrchestrator = agentOrchestrator;
         _toolGateway = toolGateway;
         _governanceService = governanceService;
         _artifactRepository = artifactRepository;
         _telemetryService = telemetryService;
         _logger = logger;
      }

      public async Task ExecuteConstructionWorkflowAsync(string transactionId, CanonicalSemanticModel activeModel, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Execution Service starting Autonomous Development Loop for Transaction {TransactionId}", transactionId);

         var taskGraph = await _planningEngine.GenerateTaskGraphAsync(activeModel);

         // Iterate over the dictionary of tasks
         foreach (var taskEntry in taskGraph.Tasks)
         {
            var task = taskEntry.Value;
            await ProcessConstructionTaskAsync(transactionId, task, activeModel, cancellationToken);
         }

         await _artifactRepository.CommitChangesAsync(transactionId, "Autonomous construction cycle completed successfully.");
         var deploymentPackage = await _artifactRepository.CreateDeploymentPackageAsync(transactionId);

         _logger.LogInformation("Autonomous Construction completed. Deployment Package {PackageId} is ready for operations.", deploymentPackage.PackageId);
      }

      private async Task ProcessConstructionTaskAsync(string transactionId, ConstructionTask task, CanonicalSemanticModel model, CancellationToken cancellationToken)
      {
         int repairAttempts = 0;
         bool isStable = false;
         string priorValidationFeedback = string.Empty;

         while (!isStable && repairAttempts < MaxRepairCycles)
         {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Agent Generation
            var agentContext = new AgentContext { DeterministicValidationFeedback = priorValidationFeedback };

            _telemetryService.RecordEvent(new ExecutionTelemetry
            {
               EventId = $"TEL-EXEC-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
               TransactionId = transactionId,
               TaskId = task.TaskId,
               ExecutionStatus = repairAttempts == 0 ? "Generating" : "InRepair",
               Timestamp = DateTimeOffset.UtcNow
            });

            var agentResult = await _agentOrchestrator.DispatchTaskAsync(task, agentContext, cancellationToken);

            if (!agentResult.IsSuccess && agentResult.ErrorMessage == "ESCALATION_REQUIRED")
            {
               TriggerHumanMachineEscalation(transactionId, task.TaskId, "Missing canonical context or ambiguous standard", "Agent Advisory Collaboration Triggered");
               return;
            }

            // 2. Deterministic Tool Validation using the revised ValidationRequest
            var validationRequest = new ValidationRequest
            {
               TransactionId = transactionId,
               GeneratingTaskId = task.TaskId,
               ArtifactContent = agentResult.GeneratedArtifacts,
               TargetTraceabilityId = task.TargetTraceabilityId,
               ValidationRuleId = $"VAL-AUTO-{task.TargetTraceabilityId}"
            };

            // The ToolGateway returns your exact ValidationResult
            ValidationResult toolResult = await _toolGateway.InvokeValidationAsync(task.TargetTraceabilityId, validationRequest);

            _telemetryService.RecordEvent(new ToolInteractionTelemetry
            {
               EventId = $"TEL-TOOL-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
               TransactionId = transactionId,
               ToolName = $"PluginAdapter-[{toolResult.ValidationRuleId}]", // Derived from RuleId since ToolName is omitted from result
               TargetTraceabilityId = task.TargetTraceabilityId,
               IsSuccessful = toolResult.IsSuccessful,
               Timestamp = DateTimeOffset.UtcNow
            });

            // FIX: Structurally parse the explicit Errors and SecurityFindings lists into the feedback context
            if (!toolResult.IsSuccessful)
            {
               var feedbackLines = new List<string>();

               if (toolResult.Errors.Any())
               {
                  feedbackLines.Add($"COMPILATION/STRUCTURAL ERRORS: {string.Join(" | ", toolResult.Errors)}");
               }

               if (toolResult.SecurityFindings.Any())
               {
                  feedbackLines.Add($"SECURITY VULNERABILITIES: {string.Join(" | ", toolResult.SecurityFindings)}");
               }

               priorValidationFeedback = string.Join("\n", feedbackLines);
               repairAttempts++;
               _logger.LogWarning("Task {TaskId} failed deterministic validation. Entering Repair Cycle {Cycle}", task.TaskId, repairAttempts);
               continue;
            }

            // 3. Continuous Governance & Policy Enforcement
            bool governanceFailed = false;

            var policyEntity = model.Policies.FirstOrDefault(p => p.TraceabilityId == task.TargetTraceabilityId);
            var targetPolicy = new GovernancePolicy
            {
               PolicyId = policyEntity?.TraceabilityId ?? "DEFAULT",
               Description = policyEntity?.Name ?? "Unmapped Policy",
               ComplianceTier = "Optional"
            };

            foreach (var artifactEntry in agentResult.GeneratedArtifacts)
            {
               string artifactId = artifactEntry.Key;
               string artifactContent = artifactEntry.Value;

               var complianceResult = await _governanceService.EvaluateComplianceAsync(artifactId, targetPolicy);

               if (!complianceResult.IsCompliant)
               {
                  governanceFailed = true;
                  priorValidationFeedback = string.Join(";", complianceResult.Violations);
                  break;
               }

               // 4. Artifact Finalization
               var finalArtifact = new SoftwareArtifact
               {
                  ArtifactId = artifactId,
                  TransactionId = transactionId,
                  FilePath = $"/src/Generated/{artifactId}.cs",
                  Content = artifactContent,
                  Category = "SourceCode",
                  SourceTraceabilityId = task.TargetTraceabilityId,
                  GeneratingTaskId = task.TaskId,
                  GeneratingAgentRole = "Implementation Generator"
               };

               await _artifactRepository.SaveArtifactAsync(finalArtifact);
            }

            if (governanceFailed)
            {
               repairAttempts++;
               _logger.LogWarning("Task {TaskId} failed governance checks. Entering Repair Cycle {Cycle}", task.TaskId, repairAttempts);
               continue;
            }

            // Both Tool and Governance validation cleared
            isStable = true;
            _telemetryService.RecordEvent(new ExecutionTelemetry
            {
               EventId = $"TEL-EXEC-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
               TransactionId = transactionId,
               TaskId = task.TaskId,
               ExecutionStatus = "Completed",
               Timestamp = DateTimeOffset.UtcNow
            });
         }

         // 5. Human-Machine Escalation (The "Andon Cord")
         if (!isStable)
         {
            TriggerHumanMachineEscalation(transactionId, task.TaskId, "Unresolvable Validation or Policy Conflict", priorValidationFeedback);
         }
      }

      private void TriggerHumanMachineEscalation(string transactionId, string taskId, string policyId, string failureContext)
      {
         var exceptionContext = new PolicyEvaluationResult
         {
            ArtifactId = $"FAIL-{taskId}",
            PolicyId = policyId,
            IsCompliant = false,
            Violations = new List<string> { failureContext }
         };

         _governanceService.EscalateToHumanGovernance(transactionId, exceptionContext);

         throw new InvalidOperationException($"Autonomous execution halted on Task {taskId}. Human Governance escalation required.");
      }
   }
}
