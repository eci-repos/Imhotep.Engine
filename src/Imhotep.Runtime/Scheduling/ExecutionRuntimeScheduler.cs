using Imhotep.Agents.Abstractions;
using Imhotep.Orchestration.Services;
using Imhotep.Planning.Models;
using Imhotep.Tools.Gateway;
using Imhotep.Common.Models;
using Imhotep.Agents.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Runtime.Scheduling
{
   public interface ITaskScheduler
   {
      Task ExecuteTaskGraphAsync(IReadOnlyList<ConstructionTask> taskGraph, AgentContext baseContext, CancellationToken cancellationToken = default);
   }

   /// <summary>
   /// Coordinates the execution of the construction task graph, managing agent invocation, 
   /// deterministic validation, and automated repair cycles (ISL v2.4 & v3.5).
   /// </summary>
   public class ExecutionRuntimeScheduler : ITaskScheduler
   {
      private readonly IAgentOrchestrator _agentOrchestrator;
      private readonly IToolGateway _toolGateway;
      private readonly ILogger<ExecutionRuntimeScheduler> _logger;

      public ExecutionRuntimeScheduler(
          IAgentOrchestrator agentOrchestrator,
          IToolGateway toolGateway,
          ILogger<ExecutionRuntimeScheduler> logger)
      {
         _agentOrchestrator = agentOrchestrator;
         _toolGateway = toolGateway;
         _logger = logger;
      }

      public async Task ExecuteTaskGraphAsync(IReadOnlyList<ConstructionTask> taskGraph, AgentContext baseContext, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Execution Runtime initiating processing for {Count} tasks.", taskGraph.Count);

         // Execute the autonomous development loop until convergence or escalation (ISL v3.6)
         while (taskGraph.Any(t => t.ExecutionStatus == PlanTaskStatus.Pending || t.ExecutionStatus == PlanTaskStatus.InRepair || t.ExecutionStatus == PlanTaskStatus.InProgress))
         {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Dependency Resolution: Find tasks whose prerequisites are met
            var executableTasks = GetExecutableTasks(taskGraph);

            if (!executableTasks.Any() && taskGraph.Any(t => t.ExecutionStatus == PlanTaskStatus.Pending))
            {
               _logger.LogError("Task Graph deadlocked: Pending tasks exist but dependencies are unmet.");
               throw new InvalidOperationException("Structural deadlock detected in Construction Task Graph.");
            }

            if (!executableTasks.Any())
            {
               // Allow currently executing asynchronous tasks to finish
               await Task.Delay(1000, cancellationToken);
               continue;
            }

            // 2. Parallel Task Execution (ISL v3.5)
            var executionTasks = executableTasks.Select(task => ProcessTaskLifecycleAsync(task, baseContext, cancellationToken));
            await Task.WhenAll(executionTasks);
         }

         _logger.LogInformation("Execution Runtime has finalized the task graph.");
      }

      /// <summary>
      /// Identifies tasks that are ready to be executed based on dependency resolution.
      /// </summary>
      private IReadOnlyList<ConstructionTask> GetExecutableTasks(IReadOnlyList<ConstructionTask> taskGraph)
      {
         return taskGraph
             .Where(t => t.ExecutionStatus == PlanTaskStatus.Pending || t.ExecutionStatus == PlanTaskStatus.InRepair)
             .Where(t => !t.Dependencies.Any() || t.Dependencies.All(depId =>
                 taskGraph.FirstOrDefault(gt => gt.TaskId == depId)?.ExecutionStatus == PlanTaskStatus.Completed))
             .ToList();
      }

      /// <summary>
      /// Manages the full lifecycle of a single task: Agent Generation -> Deterministic Tool Validation -> Repair Cycle Routing.
      /// </summary>
      private async Task ProcessTaskLifecycleAsync(ConstructionTask task, AgentContext context, CancellationToken cancellationToken)
      {
         task.ExecutionStatus = PlanTaskStatus.InProgress;
         _logger.LogInformation("Executing Task {TaskId} for TraceabilityId {TraceabilityId}", task.TaskId, task.TargetTraceabilityId);

         try
         {
            // 3. Agent Invocation Pipeline: Dispatch to Implementation Generator or Repair Analyst
            AgentResult agentResult = await _agentOrchestrator.DispatchTaskAsync(task, context, cancellationToken);

            // 4. Human-Machine Escalation ("Andon Cord") Enforcement
            if (!agentResult.IsSuccess && agentResult.ErrorMessage == "ESCALATION_REQUIRED")
            {
               _logger.LogError("Task {TaskId} triggered a Human-Machine Escalation.", task.TaskId);
               task.ExecutionStatus = PlanTaskStatus.Escalated;
               return; // Halt autonomous execution for this specific branch
            }

            // 5. Deterministic Validation: If code was generated, test it via the Tool Gateway
            if (task.Category == TaskCategory.Implementation && agentResult.IsSuccess && agentResult.GeneratedArtifacts.Any())
            {
               // Retrieve the mapped validation rule from the Semantic Model (mocked here as VAL-001)
               var validationRequest = new ValidationRequest
               {
                  TransactionId = context.SemanticModel.TransactionId, // The active transaction context (e.g., from your semantic model or graph)
                  GeneratingTaskId = task.TaskId,            // The explicit task that generated this code
                  ArtifactContent = agentResult.GeneratedArtifacts,
                  TargetTraceabilityId = task.TargetTraceabilityId,
                  ValidationRuleId = "VAL-001"
               };

               ValidationResult validationResult = await _toolGateway.InvokeValidationAsync("Compile.DotNet", validationRequest);

               // 6. Automated Repair Cycle Routing
               if (!validationResult.IsSuccessful)
               {
                  _logger.LogWarning("Task {TaskId} failed deterministic validation. Routing to Repair Analyst.", task.TaskId);

                  task.ExecutionStatus = PlanTaskStatus.InRepair;
                  task.RepairAttempts++;

                  // Inject the precise failure context so the Repair Analyst doesn't hallucinate
                  context.DeterministicValidationFeedback = string.Join("\n", validationResult.Errors);
                  context.ValidationRuleId = validationResult.ValidationRuleId;

                  return; // Re-enters the queue as 'InRepair' for the next loop iteration
               }
            }

            // If we reach here, the task either didn't need validation (e.g., planning) or passed all tool checks
            task.ExecutionStatus = PlanTaskStatus.Completed;

            // Clear out repair state in the context in case this context object is reused
            context.DeterministicValidationFeedback = null;
            context.ValidationRuleId = null;

            _logger.LogInformation("Task {TaskId} successfully completed and verified.", task.TaskId);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Catastrophic failure processing Task {TaskId}", task.TaskId);
            task.ExecutionStatus = PlanTaskStatus.Escalated;
         }
      }
   }
}
