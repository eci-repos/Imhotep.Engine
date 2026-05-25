using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.Common.Models;
using Imhotep.Governance.Models;
using Imhotep.Governance.Services;
using Imhotep.Orchestration.Services;
using Imhotep.Planning.Models;
using Imhotep.State.Models;
using Imhotep.State.Services;
using Imhotep.Tools.Gateway;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Runtime.Scheduling;

/// <summary>
/// ISL v2.4: Execution Runtime Model.
/// Coordinates the execution of the construction task graph, managing agent invocation, 
/// deterministic validation, and automated repair cycles.
/// </summary>
public interface ITaskScheduler
{
   /// <summary>
   /// Executes the autonomous development loop for a given construction task graph.
   /// </summary>
   /// <param name="taskGraph">The formal ISL v1.5 Construction Task Graph containing the plan to execute.</param>
   /// <param name="transactionId">The correlation identifier used for traceability and execution telemetry.</param>
   /// <param name="cancellationToken">Cancellation token to observe.</param>
   Task ExecuteTaskGraphAsync(
       ConstructionTaskGraph taskGraph,
       string transactionId,
       CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates the execution of the construction task graph, managing agent invocation, 
/// deterministic validation, and automated repair cycles (ISL v2.4 & v3.5).
/// </summary>
public class ExecutionRuntimeScheduler : ITaskScheduler
{
   private readonly IAgentOrchestrator _agentOrchestrator;
   private readonly IToolGateway _toolGateway;
   private readonly IGovernanceService _governanceService; // REQUIRED: For Approval Gates
   private readonly IStateManager _stateManager;           // REQUIRED: For Durable State
   private readonly ILogger<ExecutionRuntimeScheduler> _logger;

   public ExecutionRuntimeScheduler(
       IAgentOrchestrator agentOrchestrator,
       IToolGateway toolGateway,
       IGovernanceService governanceService,
       IStateManager stateManager,
       ILogger<ExecutionRuntimeScheduler> logger)
   {
      _agentOrchestrator = agentOrchestrator;
      _toolGateway = toolGateway;
      _governanceService = governanceService;
      _stateManager = stateManager;
      _logger = logger;
   }

   // Accept ConstructionTaskGraph instead of a list, and remove global AgentContextPackage
   public async Task ExecuteTaskGraphAsync(
       ConstructionTaskGraph taskGraph,
       string transactionId,
       CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Execution Runtime initiating processing for Plan {PlanId}.", taskGraph.PlanId);

      bool isGraphComplete = false;

      // Execute the autonomous development loop until convergence or escalation (ISL v3.6)
      while (!isGraphComplete)
      {
         cancellationToken.ThrowIfCancellationRequested();

         // 1. ISL v3.5 Sec 3.3: Read from durable state, not just an in-memory variable
         var currentGraphState = await _stateManager.GetExecutionGraphAsync(taskGraph.PlanId, cancellationToken);

         var pendingTasks = currentGraphState.Tasks.Where(t =>
             t.Status == PlanStatus.Pending || t.Status == PlanStatus.InRepair).ToList();

         var inProgressTasks = currentGraphState.Tasks.Where(t => t.Status == PlanStatus.InProgress).ToList();

         if (!pendingTasks.Any())
         {
            if (!inProgressTasks.Any())
            {
               isGraphComplete = true; // All tasks converged, escalated, or failed
               break;
            }
            // Allow currently executing asynchronous tasks to finish
            await Task.Delay(1000, cancellationToken);
            continue;
         }
         // 2. ISL v3.5 Sec 8.0: Eligibility Evaluator (Dependencies AND Governance)
         var executableTasks = new List<ConstructionTask>();
         foreach (var task in GetExecutableTasks(currentGraphState.Tasks))
         {
            // 3. ISL v1.7 Sec 16.0: Runtime Governance Enforcement Check [1]
            var govRequest = new GovernanceCheckRequest
            {
               CheckId = $"GOV-CHK-{Guid.NewGuid():N}",
               CheckType = "execution",
               SpecificationId = currentGraphState.SpecificationId, // Pulled safely from the graph state
               SpecificationVersion = currentGraphState.SpecificationVersion,
               TargetId = task.TaskId,
               TargetType = "task",
               RequestedAction = "dispatch",
               RequestedBy = "ExecutionRuntimeScheduler",
               RequestedAt = DateTimeOffset.UtcNow
            };

            var governanceDecision = await _governanceService.EvaluateGovernanceCheckAsync(govRequest, cancellationToken);

            // ISL v1.7 Sec 16.3: Governance Decision Handling [3]
            if (governanceDecision.Decision.Equals("allow", StringComparison.OrdinalIgnoreCase))
            {
               executableTasks.Add(task);
            }
            else if (governanceDecision.Decision.Equals("escalate", StringComparison.OrdinalIgnoreCase) ||
                     governanceDecision.Decision.Equals("approval-required", StringComparison.OrdinalIgnoreCase) ||
                     governanceDecision.Decision.Equals("block", StringComparison.OrdinalIgnoreCase))
            {
               _logger.LogWarning("Task {TaskId} blocked by Governance: {Decision}. Rationale: {Rationale}",
                   task.TaskId, governanceDecision.Decision, governanceDecision.Rationale);
               // Do NOT add to executable tasks; let the governance engine handle the pause
            }
         }

         if (!executableTasks.Any() && pendingTasks.Any())
         {
            _logger.LogError("Task Graph {PlanId} deadlocked: Pending tasks exist but dependencies are unmet or blocked by governance.", taskGraph.PlanId);
            throw new InvalidOperationException("Structural deadlock or unresolved governance block detected in Construction Task Graph.");
         }

         // 4. Parallel Task Execution (ISL v3.5)
         // Notice: The orchestrator passes the task to the lifecycle processor, 
         // which will call IAgentOrchestrator.AssembleContextAsync to build the task-specific context!
         var executionTasks = executableTasks.Select(task =>
             ProcessTaskLifecycleAsync(task, currentGraphState, transactionId, cancellationToken));

         await Task.WhenAll(executionTasks);
      }

      _logger.LogInformation("Execution Runtime has finalized the task graph for Plan {PlanId}.", taskGraph.PlanId);
   }

   /// <summary>
   /// ISL v3.5 Sec 8.0: Eligibility Evaluator.
   /// Identifies tasks that are ready to be executed based on dependency resolution.
   /// </summary>
   private IReadOnlyList<ConstructionTask> GetExecutableTasks(IReadOnlyList<ConstructionTask> taskGraph)
   {
      return taskGraph
          // 1. ISL v3.5 Sec 8.1: Task state must be 'pending' or 'in-repair' (or retry-authorized)
          .Where(t => t.Status == PlanStatus.Pending || t.Status == PlanStatus.InRepair)

          // 2. ISL v3.5 Sec 8.1: All required predecessor tasks must be satisfied.
          .Where(t => t.Dependencies == null || !t.Dependencies.Any() || t.Dependencies.All(depId =>
          {
             var predecessor = taskGraph.FirstOrDefault(gt => gt.TaskId == depId);

             // ISL v1.5 Sec 13.5: If a dependency cannot be resolved, the affected task is blocked
             if (predecessor == null)
             {
                _logger.LogError("Unresolved dependency {DepId} found. Task cannot execute.", depId);
                return false;
             }

             // ISL v2.4 Sec 29.1 & ISL v1.5 Sec 23.1: Both 'completed' and 'skipped' satisfy dependency execution criteria
             return predecessor.Status == PlanStatus.Completed || predecessor.Status == PlanStatus.Skipped;
          }))
          .ToList();
   }

   /// <summary>
   /// ISL v2.4 Sec 17.0: Task Execution Lifecycle.
   /// Manages the bounded execution of a single task using immutable state transitions.
   /// </summary>
   private async Task ProcessTaskLifecycleAsync(
       ConstructionTask task,
       ConstructionTaskGraph currentGraphState,
       string transactionId,
       CancellationToken cancellationToken = default)
   {
      // 1. Immutable State Transition [ISL v2.2 Sec 5.3]
      // Use the 'with' expression to generate a new task instance reflecting the new state
      var inProgressTask = task with { Status = PlanStatus.InProgress };

      // Persist the transition explicitly to the durable State Manager
      // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, inProgressTask, cancellationToken);

      string sourceEntities = inProgressTask.SourceEntityIds != null ? string.Join(", ", inProgressTask.SourceEntityIds) : "NONE";
      _logger.LogInformation("Executing Task {TaskId} fulfilling Source Entities [{SourceEntities}]", inProgressTask.TaskId, sourceEntities);

      try
      {
         // Pass the updated inProgressTask instance
         AgentContextPackage contextPackage = await _agentOrchestrator.AssembleContextAsync(inProgressTask, "Auto-Determined", cancellationToken);
         AgentOutputRecord agentOutput = await _agentOrchestrator.InvokeAgentAsync(inProgressTask, contextPackage, cancellationToken);

         if (agentOutput.OutputStatus.Equals("escalated", StringComparison.OrdinalIgnoreCase))
         {
            _logger.LogError("Task {TaskId} triggered Escalation. Reason: {Summary}", inProgressTask.TaskId, agentOutput.Summary);

            // Transition to Escalated
            var escalatedTask = inProgressTask with { Status = PlanStatus.Escalated };
            // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, escalatedTask, cancellationToken);
            return;
         }

         // 5. ISL v1.6 & v2.4 Sec 14.0: Deterministic Validation Supremacy
         if (agentOutput.RequiresDeterministicValidation)
         {
            _logger.LogInformation("Agent generated artifacts for Task {TaskId}. Routing to Tool Gateway.", inProgressTask.TaskId);

            string requiredCapability = inProgressTask.RequiredToolCapabilities?.FirstOrDefault() ?? "compile";

            // Construct ISL v1.6 compliant Tool Invocation Request 
            // All 'required' properties strictly populated to satisfy C# compiler and Zero-Trust constraints
            var toolRequest = new ToolInvocationRequest
            {
               ToolInvocationId = $"TIV-{Guid.NewGuid():N}",

               // Tool Selection & Versioning Metadata [ISL v1.6 Sec 10.0 & 17.0]
               ToolSelectionId = $"SEL-{Guid.NewGuid():N}",
               ToolPluginId = "PLG-DOTNET-CLI-001",
               PluginVersion = "1.0.0",
               ToolName = "dotnet",
               ToolVersion = "8.0",
               CapabilityName = requiredCapability,

               // Task & Execution Context [ISL v1.6 Sec 11.1]
               TaskId = inProgressTask.TaskId,
               ExecutionGraphId = currentGraphState.PlanId, // Maps to the active execution graph
               WorkItemId = null, // Optional in this context

               // Specification Context
               SpecificationId = contextPackage.SpecificationId,
               SpecificationVersion = contextPackage.SpecificationVersion,

               // Artifact & Input Targets
               ArtifactIds = agentOutput.ProducedArtifactCandidates,
               InputReferences = agentOutput.ProducedArtifactCandidates ?? new List<string>(),

               // Strict Execution & Isolation Profiles [ISL v1.6 Sec 17.1 & ISL v3.10 Sec 16.1]
               EnvironmentProfileId = "ENV-MACS-LOCAL",
               IsolationProfileId = "sandboxed-generated-code", // Forces the tool into a restricted workspace

               // Operational Controls
               TimeoutSeconds = 120,
               DryRun = false,
               Parameters = null,
               GovernanceCheckId = null, // Populated if this specific tool required a pre-approval
               CorrelationId = transactionId,

               RequestedAt = DateTimeOffset.UtcNow,
               RequestedBy = "ExecutionRuntimeScheduler"
            };

            // Invoke deterministic validation via the Tool Gateway
            ToolInvocationResult toolResult = await _toolGateway.InvokeToolAsync(toolRequest, cancellationToken);

            // 6. ISL v2.4 Sec 15.0: Automated Repair Cycle Routing
            if (!toolResult.Outcome.Equals("passed", StringComparison.OrdinalIgnoreCase) &&
                !toolResult.Outcome.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
               _logger.LogWarning("Task {TaskId} failed deterministic validation. Outcome: {Outcome}.", inProgressTask.TaskId, toolResult.Outcome);

               // ISL v1.2 Sec 15.3 & ISL v2.2 Sec 18.1: Determine repair iteration from durable state
               // We query the StateManager to count past failures instead of storing it on the static plan.
               // (For the POC, you can implement a simple in-memory list or dictionary in your StateManager mock).
               var pastRepairRecords = await _stateManager.GetRepairRecordsForTaskAsync(inProgressTask.TaskId, cancellationToken);
               int currentIteration = pastRepairRecords.Count + 1;

               // ISL v1.2 Sec 15.2: Repair Termination Policy
               if (currentIteration >= 5)
               {
                  _logger.LogError("Task {TaskId} exceeded maximum repair limits (Attempt {Iteration}). Pulling Andon Cord.", inProgressTask.TaskId, currentIteration);
                  var failedTask = inProgressTask with { Status = PlanStatus.Escalated };
                  // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, failedTask, cancellationToken);
                  return;
               }

               // Generate the formal Repair Record to durably track this attempt
               var repairRecord = new RepairRecord
               {
                  RepairId = $"RPR-{Guid.NewGuid():N}",
                  TaskId = inProgressTask.TaskId,
                  ArtifactId = agentOutput.ProducedArtifactCandidates?.FirstOrDefault() ?? "UNKNOWN",
                  FailedValidationResultId = toolResult.ToolInvocationResultId ?? "UNKNOWN",
                  Iteration = currentIteration,
                  Outcome = "unresolved",
                  RecordedAt = DateTimeOffset.UtcNow
               };

               // Persist the repair record so the next loop knows the new iteration count
               // await _stateManager.SaveRepairRecordAsync(repairRecord, cancellationToken);

               // Transition the task to InRepair using the 'with' expression
               var repairingTask = inProgressTask with { Status = PlanStatus.InRepair };
               // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, repairingTask, cancellationToken);
               return;
            }
         }

         // Transition to Completed
         var completedTask = inProgressTask with { Status = PlanStatus.Completed };
         // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, completedTask, cancellationToken);

         _logger.LogInformation("Task {TaskId} successfully completed and verified.", inProgressTask.TaskId);
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Catastrophic runtime failure processing Task {TaskId}", inProgressTask.TaskId);
         var crashedTask = inProgressTask with { Status = PlanStatus.Escalated };
         // await _stateManager.UpdateTaskStateAsync(currentGraphState.PlanId, crashedTask, cancellationToken);
      }
   }

}
