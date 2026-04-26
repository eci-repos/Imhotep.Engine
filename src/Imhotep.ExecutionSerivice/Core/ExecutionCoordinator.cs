
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Imhotep.SemanticModel.Graph;
using Imhotep.Planning.Models;
using Imhotep.Governance.Services;
using Imhotep.Observability.Services;
using Imhotep.Observability.Models;
using Imhotep.Repository.Services;
using Imhotep.Runtime.Models;
using Imhotep.Runtime.Services;
using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.Tools.Abstractions;
using Imhotep.Planning.Services;

namespace Imhotep.ExecutionService.Core;

/// <summary>
/// The central orchestrator that executes the Autonomous Development Loop.
/// Integrates Planning, Agent Orchestration, Deterministic Tools, and Governance.
/// </summary>
public class ExecutionCoordinator : IExecutionRuntime
{
   private readonly IPlanningEngine _planningEngine;
   private readonly IAgentOrchestrator _agentOrchestrator;
   private readonly IValidationPlugin _toolGateway;
   private readonly IGovernanceService _governanceService;
   private readonly IArtifactRepository _artifactRepository;
   private readonly ITelemetryService _telemetryService;
   private readonly ILogger<ExecutionCoordinator> _logger;

   public ExecutionCoordinator(
       IPlanningEngine planningEngine,
       IAgentOrchestrator agentOrchestrator,
       IValidationPlugin toolGateway,
       IGovernanceService governanceService,
       IArtifactRepository artifactRepository,
       ITelemetryService telemetryService,
       ILogger<ExecutionCoordinator> logger)
   {
      _planningEngine = planningEngine;
      _agentOrchestrator = agentOrchestrator;
      _toolGateway = toolGateway;
      _governanceService = governanceService;
      _artifactRepository = artifactRepository;
      _telemetryService = telemetryService;
      _logger = logger;
   }

   public async Task<ExecutionState> ExecuteConstructionPlanAsync(
       ConstructionTaskGraph taskGraph,
       CanonicalSemanticModel semanticModel)
   {
      _logger.LogInformation("Initiating autonomous construction for Transaction: {TransactionId}", semanticModel.TransactionId);

      // 1. Governance Enforcement: Verify the specification has passed human Approval Gates [8]
      var approvalStatus = await _governanceService.GetApprovalGateStatusAsync(semanticModel.TransactionId, "GATE-AUTONOMOUS-READY");
      if (approvalStatus.Status != "Approved")
      {
         throw new UnauthorizedAccessException("Execution halted: Specification has not passed the required Approval Gates.");
      }

      // 2. Stream Execution Telemetry to the "Watchtower" [9]
      _telemetryService.RecordEvent(new ExecutionTelemetry
      {
         EventId = Guid.NewGuid().ToString(),
         Timestamp = DateTimeOffset.UtcNow,
         TransactionId = semanticModel.TransactionId,
         TaskId = "SYSTEM-INIT",
         ExecutionStatus = "Started"
      });

      // 3. Task Graph Execution Loop [10]
      foreach (var task in taskGraph.Tasks)
      {
         int repairCycles = 0;
         bool taskConverged = false;

         while (!taskConverged && repairCycles < 3) // Limit repair loops to prevent infinite guessing [11, 12]
         {
            // A. Agent Invocation: Dispatch the reasoning agent to generate the artifact [13]
            var agentContext = new AgentContext
            {
               SemanticModel = semanticModel
            };
            var agentResponse = await _agentOrchestrator.DispatchTaskAsync(task.Value, agentContext);

            // B. Deterministic Tool Validation: Verify the generated code [14]
            var validationRequest = new Common.Models.ValidationRequest
            {
               TransactionId = semanticModel.TransactionId,
               GeneratingTaskId = task.Value.TaskId,
               ArtifactContent = agentResponse.GeneratedArtifacts,
               TargetTraceabilityId = agentResponse.TargetTraceabilityId,
               ValidationRuleId = "VAL-DEFAULT"
            };

            var validationResult = await _toolGateway.ExecuteValidationAsync(validationRequest);

            if (validationResult.IsSuccessful)
            {
               // C. Save the verified artifact to the version-controlled Repository [15]
               await _artifactRepository.SaveArtifactAsync(new Repository.Models.SoftwareArtifact
               {
                  ArtifactId = Guid.NewGuid().ToString(),
                  TransactionId = semanticModel.TransactionId,
                  FilePath = $"/src/{task.Value.Category}/{task.Value.TaskId}.cs",
                  Content = JsonSerializer.Serialize(agentResponse.GeneratedArtifacts),
                  Category = task.Value.Category.ToString(),
                  SourceTraceabilityId = agentResponse.TargetTraceabilityId,
                  GeneratingTaskId = task.Value.TaskId,
                  GeneratingAgentRole = "ImplementationGenerator"
               });

               taskConverged = true;
            }
            else
            {
               // D. Automated Repair Cycle: Validation failed, trigger the Repair Analyst [11]
               repairCycles++;
               _logger.LogWarning("Validation failed for Task {TaskId}. Initiating Repair Cycle {Cycle}.", task.Value.TaskId, repairCycles);

               // In a full implementation, the validationResult.Errors would be passed back 
               // into the AgentContext for the Repair Analyst to diagnose.
            }
         }

         // E. Human-Machine Escalation (The "Andon Cord") [12]
         if (!taskConverged)
         {
            _logger.LogError("Task {TaskId} failed to converge after maximum repair cycles. Escalating to Human Governance.", task.Value.TaskId);
            _governanceService.EscalateToHumanGovernance(
               semanticModel.TransactionId, new Governance.Models.PolicyEvaluationResult
            {
               ArtifactId = task.Value.TaskId,
               PolicyId = "SYSTEM-CONVERGENCE",
               IsCompliant = false
            });

            throw new InvalidOperationException($"Construction halted: Task {task.Value.TaskId} requires human architectural resolution.");
         }
      }

      // 4. Finalize Deployment Package [16]
      var deploymentPackage = await _artifactRepository.CreateDeploymentPackageAsync(semanticModel.TransactionId);

      return new ExecutionState
      {
         TransactionId = semanticModel.TransactionId,
         IsWorkflowComplete = true
      };
   }

   public Task<ExecutionState> GetCurrentStateAsync(string transactionId)
   {
      // Retrieves the persistent state from memory to support workflow resumption [17]
      throw new NotImplementedException("State retrieval implementation connects to the local-first data store.");
   }
}

