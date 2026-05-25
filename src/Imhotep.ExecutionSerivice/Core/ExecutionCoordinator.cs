using Imhotep.Planning.Models;
using Imhotep.Runtime.Models;
using Imhotep.SemanticModel.Graph;
using Imhotep.State.Abstractions;
using Imhotep.State.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Runtime.Services
{
   /// <summary>
   /// ISL v2.4 & v3.5: Execution Runtime Orchestrator.
   /// Acts as the "construction foreman" coordinating tasks, agents, tools, and repair loops.
   /// </summary>
   public class ExecutionCoordinator : IExecutionRuntime
   {
      // 1. ISL v2.2: Inject the explicit logical state store for durable execution memory
      private readonly ILogicalStateStore<ExecutionState> _stateStore;
      private readonly ILogger<ExecutionCoordinator> _logger;

      // Note: In your full implementation, this will also inject the IAgentOrchestrator, IToolGateway, 
      // IArtifactRepository, and other dependencies needed by ExecuteBoundaryAsync and ProcessConstructionTaskAsync.
      public ExecutionCoordinator(
          ILogicalStateStore<ExecutionState> stateStore,
          ILogger<ExecutionCoordinator> logger)
      {
         _stateStore = stateStore;
         _logger = logger;
      }

      /// <summary>
      /// ISL v2.4 Sec 10.0 & ISL v3.5 Sec 5.0: Executes the complete sequence of construction activities.
      /// </summary>
      public async Task<ExecutionState> ExecuteConstructionPlanAsync(
          ConstructionTaskGraph taskGraph,
          CanonicalSemanticModel semanticModel,
          CancellationToken stoppingToken = default)
      {
         _logger.LogInformation("Initiating Execution Runtime for Plan {PlanId}", taskGraph.PlanId);

         // 1. ISL v2.4 Sec 10.0 & ISL v3.5 Sec 6.0: Execution Admission
         // The runtime MUST reject execution if the specification is not Autonomous-Ready
         if (!semanticModel.Project.ReadinessLevel.Equals("autonomous-ready", StringComparison.OrdinalIgnoreCase))
         {
            _logger.LogError("Admission Failed: Specification {SystemId} is at readiness level '{Readiness}'.",
                semanticModel.SystemId, semanticModel.Project.ReadinessLevel);

            throw new InvalidOperationException($"ISL v1.3 & v2.4 Violation: Cannot execute construction plan. Specification MUST be 'autonomous-ready'.");
         }
         // 2. ISL v2.2 Sec 14.0: Initialize Execution State
         // The execution state MUST be recorded in the persistent memory model before starting tasks
         string transactionId = $"EXEC-{Guid.NewGuid():N}";
         var executionState = new ExecutionState
         {
            ExecutionStateId = transactionId,
            ExecutionGraphId = $"GRAPH-{Guid.NewGuid():N}",
            PlanId = taskGraph.PlanId,
            SpecificationId = semanticModel.SystemId,
            SpecificationVersion = semanticModel.Version,

            // ISL v2.4 Sec 9.1 & 11.1: Runtime graph begins in "in-progress" status
            ExecutionStatus = "in-progress",

            // Initialize immutable collections to satisfy IReadOnlyList schemas
            TaskStates = new List<TaskStateRecord>().AsReadOnly(),
            PhaseStates = new List<PhaseStateRecord>().AsReadOnly(),
            UpdatedAt = DateTimeOffset.UtcNow
         };

         // Save the initial state to the durable database using UpsertAsync
         await _stateStore.UpsertAsync(executionState.ExecutionStateId, executionState, stoppingToken);
         try
         {
            // 3. ISL v1.5 Sec 19.0: Resolve and execute Boundary-Aware topological sort
            // We resolve the boundaries to ensure we execute upstream dependencies before downstream components
            var topologicalBoundaries = ResolveTopologicalBoundaries(taskGraph);

            foreach (var boundary in topologicalBoundaries)
            {
               // Delegate to the ExecuteBoundaryAsync logic (which contains ProcessConstructionTaskAsync)
               await ExecuteBoundaryAsync(boundary, taskGraph, semanticModel, transactionId, stoppingToken);
            }

            // 4. ISL v2.4 Sec 29.0: Completion Orchestration
            // Execution MAY be marked completed only when all required tasks are completed, skipped, or waived
            _logger.LogInformation("Execution successfully completed for transaction {TransactionId}", transactionId);

            executionState = executionState with
            {
               ExecutionStatus = "completed",
               UpdatedAt = DateTimeOffset.UtcNow
            };
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Execution loop failed or triggered an Andon Cord escalation for transaction {TransactionId}", transactionId);

            // ISL v2.4 Sec 9.1: If an escalation occurs, the status must reflect "escalated".
            // Otherwise, it is a terminal "failed" status.
            string finalStatus = ex is InvalidOperationException && ex.Message.Contains("Escalation")
                ? "escalated"
                : "failed";

            executionState = executionState with
            {
               ExecutionStatus = finalStatus,
               UpdatedAt = DateTimeOffset.UtcNow
            };

            throw; // Rethrow to allow the broader application handler to process the failure
         }
         finally
         {
            // ISL v2.2 Sec 5.3: Always persist the terminal execution state transaction using UpsertAsync
            await _stateStore.UpsertAsync(executionState.ExecutionStateId, executionState, stoppingToken);
         }

         return executionState;
      }

      /// <summary>
      /// ISL v2.2: Retrieves the current persistent execution state to support observability and workflow resumption.
      /// </summary>
      public async Task<ExecutionState> GetCurrentStateAsync(string transactionId)
      {
         var state = await _stateStore.GetByIdAsync(transactionId);
         if (state == null)
         {
            throw new KeyNotFoundException($"ISL v2.2 Violation: ExecutionState {transactionId} could not be found in the durable state store.");
         }
         return state;
      }

      // --- Stubs for internal execution flow we defined in previous steps ---

      private IReadOnlyList<ConstructionBoundary> ResolveTopologicalBoundaries(ConstructionTaskGraph taskGraph)
      {
         // Implementation provided in earlier steps.
         return taskGraph.Boundaries ?? new List<ConstructionBoundary>().AsReadOnly();
      }

      private async Task ExecuteBoundaryAsync(ConstructionBoundary boundary, ConstructionTaskGraph graph, CanonicalSemanticModel activeModel, string transactionId, CancellationToken cancellationToken)
      {
         // Implementation provided in earlier steps calling VerifyBoundaryEntryCriteriaAsync, 
         // ProcessConstructionTaskAsync (Agent Orchestration / Tool Validation), and VerifyBoundaryExitCriteriaAsync.
         await Task.CompletedTask;
      }
   }
}
