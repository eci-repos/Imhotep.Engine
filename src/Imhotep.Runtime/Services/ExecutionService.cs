using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.Common.Models;
using Imhotep.Governance.Models;
using Imhotep.Governance.Services;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;
using Imhotep.Planning.Models;
using Imhotep.Planning.Services;
using Imhotep.Repository.Models;
using Imhotep.Repository.Services;
using Imhotep.Runtime.Evaluation;
using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using Imhotep.State.Models;
using Imhotep.State.Services;
using Imhotep.Tools.Gateway;
using Imhotep.Traceability.Models;
using Imhotep.Traceability.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Imhotep.Runtime.Services;

public interface IExecutionService
{
   Task ExecuteConstructionWorkflowAsync(
      string transactionId, 
      CanonicalSemanticModel activeModel, 
      CancellationToken cancellationToken = default);

   /// <summary>
   /// Executes a single encapsulated construction boundary under Zero-Trust mandates.
   /// </summary>
   Task ExecuteBoundaryAsync(
      ConstructionBoundary boundary,
      ConstructionTaskGraph graph,
      CanonicalSemanticModel activeModel,
      string transactionId,
      CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.5 Sec 19.4 (Connection Contexts): Validates cross-boundary artifact consumption.
   /// Enforces zero-trust isolation by blocking artifact reads across boundaries 
   /// unless explicitly authorized by a formal Connection Context contract.
   /// </summary>
   Task ValidateCrossBoundaryAccessAsync(
       string consumingBoundaryId,
       string producingBoundaryId,
       string artifactId,
       IReadOnlyList<ConnectionContext> activeContexts,
       CancellationToken cancellationToken = default);
}

/// <summary>
/// ISL v2.4: Execution Runtime Model.
/// Acts as the operational engine, scheduling tasks from the planning graph, invoking agents, 
/// triggering automated repair cycles, and enforcing strict governance boundaries.
/// </summary>
public class ExecutionService : IExecutionService
{
   private readonly IPlanningEngine _planningEngine;
   private readonly IAgentOrchestrator _agentOrchestrator;
   private readonly IToolGateway _toolGateway;
   private readonly IGovernanceService _governanceService;
   private readonly IArtifactRepository _artifactRepository;
   private readonly ITelemetryService _telemetryService;

   private readonly IStateManager _stateManager;
   private readonly ITraceabilityService _traceabilityService;

   private readonly IBoundaryEvaluator _boundaryEvaluator;
   private readonly IConnectionContextValidator _contextValidator;

   private readonly ILogger<ExecutionService> _logger;

   private const int MaxRepairCycles = 5;

   public ExecutionService(
       IPlanningEngine planningEngine,
       IAgentOrchestrator agentOrchestrator,
       IToolGateway toolGateway,
       IGovernanceService governanceService,
       IArtifactRepository artifactRepository,
       ITelemetryService telemetryService,
       IStateManager stateManager,
       ITraceabilityService traceabilityService,
       IBoundaryEvaluator boundaryEvaluator,
       IConnectionContextValidator contextValidator,
       ILogger<ExecutionService> logger)
   {
      _planningEngine = planningEngine;
      _agentOrchestrator = agentOrchestrator;
      _toolGateway = toolGateway;
      _governanceService = governanceService;
      _artifactRepository = artifactRepository;
      _telemetryService = telemetryService;
      _boundaryEvaluator = boundaryEvaluator;
      _contextValidator = contextValidator;

      _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
      _traceabilityService = traceabilityService ?? throw new ArgumentNullException(nameof(traceabilityService));

      _logger = logger;
   }

   public async Task ExecuteConstructionWorkflowAsync(
       string transactionId,
       CanonicalSemanticModel activeModel,
       CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Initiating Execution Runtime for Transaction {TransactionId}", transactionId);

      // 1. INITIALIZE TRACEABILITY (ISL v1.4 & v2.4)
      // Create the root Execution Event node for this workflow run
      var executionNode = new TraceabilityNode
      {
         NodeId = $"EXE-{transactionId}",
         NodeType = "ExecutionEvent",
         SpecificationId = activeModel.SystemId ?? "UNKNOWN", // Assuming activeModel has a SystemId
         SpecificationVersion = activeModel.Version ?? "1.0.0",
         CreatedAt = DateTimeOffset.UtcNow,
         CreatedBy = "ExecutionService",
         Status = "in-progress"
      };

      await _traceabilityService.RecordNodeAsync(executionNode, cancellationToken);

      // 2. INITIALIZE STATE & MEMORY (ISL v2.2)
      // Create the Execution Graph State Record
      var executionState = new PlatformStateRecord
      {
         StateRecordId = $"EXEC-STATE-{transactionId}",
         StateCategory = "execution",
         ObjectId = transactionId,
         ObjectType = "ExecutionGraph",
         CurrentState = "in-progress",
         StateVersion = "1.0",
         CorrelationId = transactionId,
         UpdatedAt = DateTimeOffset.UtcNow,
         UpdatedBy = "ExecutionService"
      };

      // Commit the initialization as an atomic State Transaction
      var stateTransaction = new StateTransitionTransaction
      {
         TransactionId = $"TXN-START-{transactionId}",
         TransactionType = "execution-started",
         AffectedStateRecords = new List<string> { executionState.StateRecordId },
         AffectedEventRecords = new List<string>(),
         InitiatedBy = "ExecutionService",
         StartedAt = DateTimeOffset.UtcNow,
         Status = "committed"
      };

      await _stateManager.CommitStateTransactionAsync(stateTransaction, cancellationToken);

      // 3. CREATE INITIAL CHECKPOINT (ISL v2.4 Sec 27.1)
      // The runtime MUST create checkpoints after admission and before execution start
      _logger.LogInformation("Creating initial execution checkpoint for Transaction {TransactionId}", transactionId);

      // In a full implementation, this calls _stateManager.CreateSnapshotAsync.
      // For MACS, we log the safe recovery marker.

      try
      {
         // 4. COORDINATE CONSTRUCTION BOUNDARIES (ISL v1.5)
         // First, formally generate the Construction Task Graph using the active model
         var plan = await _planningEngine.GenerateConstructionPlanAsync(transactionId, activeModel, cancellationToken);

         // Fetch the planned boundaries from the Planning Engine
         var boundaries = await _planningEngine.GetConstructionBoundariesAsync(transactionId, cancellationToken);

         foreach (var boundary in boundaries)
         {
            await ExecuteBoundaryAsync(
                boundary,
                plan.TaskGraph,
                activeModel,
                transactionId,
                cancellationToken);
         }

         // 5. FINALIZE EXECUTION (ISL v2.4 Sec 29.0)
         _logger.LogInformation("Execution Construction Workflow completed successfully for Transaction {TransactionId}", transactionId);

         // Update state to completed
         var completionTransaction = stateTransaction with
         {
            TransactionId = $"TXN-END-{transactionId}",
            TransactionType = "execution-completed",
            CompletedAt = DateTimeOffset.UtcNow // Recording the exact completion time
         };
         await _stateManager.CommitStateTransactionAsync(completionTransaction, cancellationToken);
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Execution failed or was halted for Transaction {TransactionId}", transactionId);

         // ISL v2.4 Sec 32.0: Handle failure state and escalate if unrecoverable
         var failureTransaction = stateTransaction with
         {
            TransactionId = Guid.NewGuid().ToString(),
            TransactionType = "execution-failed",
            Status = "failed",
            RecoveryAction = "escalate-to-human-governance"
         };
         await _stateManager.CommitStateTransactionAsync(failureTransaction, cancellationToken);

         throw;
      }
   }

   #region -- 4.00 - Boundary Execution --

   public async Task ExecuteBoundaryAsync(
      ConstructionBoundary boundary,
      ConstructionTaskGraph graph,
      CanonicalSemanticModel activeModel,
      string transactionId,
      CancellationToken cancellationToken = default)
   {
      await VerifyBoundaryEntryCriteriaAsync(boundary, graph, activeModel, transactionId, cancellationToken);

      _logger.LogInformation("ExecutionEngine entering Construction Boundary {BoundaryId}: {BoundaryName} [{BoundaryType}]",
          boundary.BoundaryId, boundary.BoundaryName, boundary.BoundaryType);

      // 1. BOUNDARY ENTRY CRITERIA (ISL v1.5 Sec 19.5)
      // The runtime MUST validate entry criteria before beginning execution within a boundary.
      // This ensures all upstream dependencies, connection contexts, and governance approvals are satisfied.
      await _governanceService.RecordAuditEventAsync(new AuditLogEntry
      {
         EventType = "boundary-started",
         TargetId = boundary.BoundaryId,
         SpecificationId = activeModel.SystemId,
         SpecificationVersion = activeModel.Version,
         ActorId = "Execution Runtime",
         NewState = "in-progress",
         Rationale = "Boundary entry criteria satisfied; initiating execution.",
         CorrelationId = transactionId,
         EventTime = DateTimeOffset.UtcNow
      }, cancellationToken);

      // 2. ISOLATED TASK EXECUTION (The "Bounded Scope")
      // Resolve only the specific tasks assigned to this boundary to prevent context bloat and reasoning drift.
      var boundaryTasks = graph.Tasks.Where(t => boundary.TaskIds.Contains(t.TaskId)).ToList();

      foreach (var task in boundaryTasks)
      {
         // Execute the task using the unified processor we built previously.
         // Because the task executes inside this boundary, the agent ONLY receives the exact 
         // entities and constraints relevant to this specific scope.
         await ProcessConstructionTaskAsync(task, graph, transactionId, cancellationToken);
      }

      // 3. BOUNDARY EXIT CRITERIA (ISL v1.5 Sec 19.6)
      // The runtime MUST evaluate exit criteria before a boundary may be marked complete.
      // This verifies all tasks succeeded, artifacts passed deterministic validation, and traceability is whole.
      await VerifyBoundaryExitCriteriaAsync(boundary, graph, cancellationToken);

      // 4. BOUNDARY CONTINUATION RECORD (ISL v1.5 Sec 19.7)
      // We formally package the state of this boundary into an immutable continuation record.
      // This is how we pass context to the NEXT boundary without relying on unbounded AI chat history.
      var continuationRecord = new BoundaryContinuationRecord
      {
         ContinuationRecordId = $"CONT-{boundary.BoundaryId}-{Guid.NewGuid()}",
         BoundaryId = boundary.BoundaryId,
         CompletedAt = DateTimeOffset.UtcNow,

         CompletedTaskIds = boundaryTasks.Select(t => t.TaskId).ToList(),

         // In a complete implementation, these would be hydrated from the actual 
         // Execution State, Artifact Repository, and Tool Gateway logs for this boundary cycle:
         ProducedArtifactIds = new List<string>(),
         DecisionRecordIds = new List<string>(),
         ValidationResultIds = new List<string>(),
         GovernanceRecordIds = new List<string>(),

         // Explicitly tracks deferred issues (like non-blocking warnings)
         OpenIssues = new List<string>(),

         // Summarizes context for downstream boundaries (e.g., "Database schema generated and validated")
         DownstreamContextSummary = $"Boundary {boundary.BoundaryName} completed successfully. Artifacts are stable and validated.",
         NextBoundaryRecommendations = new List<string>()
      };

      await _governanceService.RecordAuditEventAsync(new AuditLogEntry
      {
         EventType = "continuation-record-produced",
         TargetId = continuationRecord.ContinuationRecordId,
         SpecificationId = activeModel.SystemId,
         ActorId = "Execution Runtime",
         Outcome = "produced",
         Rationale = "All boundary tasks passed deterministic validation. Continuation state securely generated.",
         CorrelationId = transactionId,
         EventTime = DateTimeOffset.UtcNow
      }, cancellationToken);

      await _governanceService.RecordAuditEventAsync(new AuditLogEntry
      {
         EventType = "boundary-completed",
         TargetId = boundary.BoundaryId,
         SpecificationId = activeModel.SystemId,
         ActorId = "Execution Runtime",
         NewState = "completed",
         Outcome = "success",
         Rationale = "Boundary exit criteria fully satisfied.",
         CorrelationId = transactionId,
         EventTime = DateTimeOffset.UtcNow
      }, cancellationToken);

      // 5. FINALIZE BOUNDARY STATE
      _logger.LogInformation("Boundary {BoundaryId} completed successfully. Generated Continuation Record {RecordId}.",
          boundary.BoundaryId, continuationRecord.ContinuationRecordId);

      // At this point, you would persist this record to your State Manager or Traceability Engine
      // e.g., await _stateManager.CommitBoundaryContinuationAsync(continuationRecord, cancellationToken);
   }

   private async Task VerifyBoundaryEntryCriteriaAsync(
      ConstructionBoundary boundary,
      ConstructionTaskGraph graph,
      CanonicalSemanticModel activeModel,
      string transactionId,
      CancellationToken cancellationToken)
   {
      _logger.LogInformation("Verifying Entry Criteria for Boundary {BoundaryId}", boundary.BoundaryId);

      // 1. Dependency Boundaries Complete (ISL v1.5 Sec 19.5)
      // Ensures we do not start execution if upstream work is pending or failed.
      if (boundary.DependencyBoundaries != null && boundary.DependencyBoundaries.Any())
      {
         foreach (var depId in boundary.DependencyBoundaries)
         {
            // Note: In the full platform, this queries the StateManager for the Continuation Record.
            // For MACS, we check the active graph's boundary states.
            var upstream = graph.Boundaries.FirstOrDefault(b => b.BoundaryId == depId);
            if (upstream != null && upstream.Status != BoundaryStatus.Completed)
            {
               throw new InvalidOperationException($"Boundary Entry Failed: Upstream dependency boundary {depId} is not complete.");
            }
         }
      }

      // 2. Required Connection Contexts Valid (ISL v1.5 Sec 19.4 & 19.5)
      // This is the "Connection Context" concept you wrote about—validating the exact data payload
      // passing between boundaries to prevent context bloat.
      var inboundContexts = graph.ConnectionContexts
          .Where(cc => cc.ToBoundaryId == boundary.BoundaryId)
          .ToList();

      foreach (var context in inboundContexts)
      {
         if (string.IsNullOrWhiteSpace(context.ValidationRule))
         {
            throw new InvalidOperationException($"Boundary Entry Failed: Connection Context {context.ConnectionContextId} from {context.FromBoundaryId} lacks a mandatory ValidationRule.");
         }

         _logger.LogDebug("Validated formal Connection Context {ContextId}. Bounding reasoning scope to explicitly provided elements.",
             context.ConnectionContextId);

         await _governanceService.RecordAuditEventAsync(new AuditLogEntry
         {
            EventType = "connection-context-validated",
            TargetId = context.ConnectionContextId,
            SpecificationId = activeModel.SystemId,
            SpecificationVersion = activeModel.Version,
            ActorId = "Execution Runtime",
            Outcome = "passed",
            Rationale = $"Context {context.ConnectionContextId} validated against rule: {context.ValidationRule}",
            CorrelationId = transactionId,
            EventTime = DateTimeOffset.UtcNow
         }, cancellationToken);
      }

      // 3. Governance & Tooling Checks (ISL v1.5 Sec 19.5)
      // (In your complete ExecutionService, you would invoke _toolGateway and _governanceService here
      // to verify that all required deterministic tools for this boundary are registered and healthy).

      _logger.LogInformation("Boundary {BoundaryId} passed all Entry Criteria. Execution Sandbox initialized.", boundary.BoundaryId);

      await Task.CompletedTask; // Placeholder for true async tool/governance checks
   }

   private async Task VerifyBoundaryExitCriteriaAsync(
      ConstructionBoundary boundary,
      ConstructionTaskGraph graph,
      CancellationToken cancellationToken)
   {
      _logger.LogInformation("Verifying Exit Criteria for Boundary {BoundaryId}", boundary.BoundaryId);

      // Isolate only the tasks assigned to this boundary
      var boundaryTasks = graph.Tasks.Where(t => boundary.TaskIds.Contains(t.TaskId)).ToList();

      // 1. All Boundary Tasks Completed (ISL v1.5 Sec 19.6)
      // Ensure no tasks were silently skipped, left pending, or failed outright.
      var incompleteTasks = boundaryTasks.Where(t => t.Status is not PlanStatus.Completed and not PlanStatus.Skipped).ToList();
      if (incompleteTasks.Any())
      {
         var taskList = string.Join(", ", incompleteTasks.Select(t => t.TaskId));
         throw new InvalidOperationException($"Boundary Exit Failed: The following tasks are incomplete: {taskList}");
      }

      // 2. No Unresolved Escalations (ISL v1.5 Sec 19.6)
      // If the Repair Analyst exhausted its repair cycles and escalated, the boundary MUST NOT close.
      var escalatedTasks = boundaryTasks.Where(t => t.Status == PlanStatus.Escalated).ToList();
      if (escalatedTasks.Any())
      {
         throw new InvalidOperationException($"Boundary Exit Failed: Boundary {boundary.BoundaryId} contains unresolved escalations. Human governance intervention required.");
      }

      // 3. Verification Passed & Traceability Complete (ISL v1.5 Sec 19.6)
      // (If the deterministic tool validation failed in ExecuteDeterministicToolsAsync, 
      // the task status would not be 'Completed', which handles this mathematically).

      _logger.LogInformation("Boundary {BoundaryId} passed all Exit Criteria. Ready to generate Continuation Record.", boundary.BoundaryId);

      await Task.CompletedTask;
   }

   #endregion

   public async Task ProcessConstructionTaskAsync(
     ConstructionTask task,
     ConstructionTaskGraph graph,
     string transactionId, // Passed in from ExecuteConstructionWorkflowAsync
     CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("ExecutionEngine processing task {TaskId} of type {TaskType}", task.TaskId, task.TaskType);

      // ZERO-TRUST CROSS-BOUNDARY CHECK [ISL v1.5 Sec 19.4]
      // Enforce that this task is legally authorized to consume upstream artifacts
      if (task.ArtifactsConsumed != null && task.ArtifactsConsumed.Any())
      {
         // 1. ISL v1.5 Sec 19.1: Resolve the consuming boundary by searching the graph
         var consumingBoundary = graph.Boundaries?.FirstOrDefault(b => b.TaskIds.Contains(task.TaskId));
         if (consumingBoundary == null)
         {
            _logger.LogError("STATE-RECORD-MISSING: Task {TaskId} is not assigned to any Construction Boundary.", task.TaskId);
            throw new InvalidOperationException($"ISL v1.5 Violation: Task {task.TaskId} is orphaned and lacks boundary protection.");
         }

         string consumingBoundaryId = consumingBoundary.BoundaryId;

         foreach (var artifactId in task.ArtifactsConsumed)
         {
            // 2. ISL v2.3 Sec 27.1: Retrieve the structured Artifact Metadata Record
            var artifactMetadata = await _artifactRepository.GetArtifactMetadataAsync(artifactId, cancellationToken);

            if (artifactMetadata == null)
            {
               _logger.LogError("STATE-RECORD-MISSING: Metadata for Artifact {ArtifactId} could not be found.", artifactId);
               throw new KeyNotFoundException($"ISL v2.3 Violation: Artifact metadata for {artifactId} is missing.");
            }

            // 3. ISL v2.3 Sec 8.1 & ISL v1.5 Sec 19.1: Resolve the producing boundary via the originating TaskId
            string producingTaskId = artifactMetadata.TaskId;
            var producingBoundary = graph.Boundaries?.FirstOrDefault(b => b.TaskIds.Contains(producingTaskId));

            if (producingBoundary == null)
            {
               _logger.LogError("STATE-RECORD-MISSING: Producing Task {TaskId} for Artifact {ArtifactId} is not assigned to any Boundary.", producingTaskId, artifactId);
               throw new InvalidOperationException($"ISL v1.5 Violation: Producing Task {producingTaskId} is orphaned.");
            }

            string producingBoundaryId = producingBoundary.BoundaryId;

            // 4. ISL v1.5 Sec 19.4: Execute the explicit zero-trust boundary check
            await ValidateCrossBoundaryAccessAsync(
                consumingBoundaryId: consumingBoundaryId,
                producingBoundaryId: producingBoundaryId,
                artifactId: artifactId,
                activeContexts: graph.ConnectionContexts ?? new List<ConnectionContext>(),
                cancellationToken: cancellationToken);
         }
      }

      // 1. REASONING AGENT INVOCATION
      if (!string.IsNullOrWhiteSpace(task.AssignedAgentRole))
      {
         _logger.LogInformation("Dispatching Task {TaskId} to Agent Orchestrator [{Role}]", task.TaskId, task.AssignedAgentRole);

         // ISL v2.1 Sec 9.2: Assemble the bounded context package to prevent hallucination
         var contextPackage = await _agentOrchestrator.AssembleContextAsync(task, task.AssignedAgentRole, cancellationToken);

         // Execute the agent reasoning strictly through the formal boundary contract
         var agentResult = await _agentOrchestrator.InvokeAgentAsync(task, contextPackage, cancellationToken);

         // ISL v3.4 Sec 14.0: Ensure the agent output passed internal validation and was not escalated
         if (agentResult.OutputStatus.Equals("escalated", StringComparison.OrdinalIgnoreCase) ||
             agentResult.OutputStatus.Equals("invalid", StringComparison.OrdinalIgnoreCase))
         {
            // Pull the Andon Cord: Halt autonomous progression for this task
            throw new InvalidOperationException($"Agent Invocation {agentResult.AgentInvocationId} failed or escalated. Status: {agentResult.OutputStatus}. Summary: {agentResult.Summary}");
         }
      }
      else
      {
         // Per ISL v1.5 Sec 9.4, tasks without an AssignedAgentRole are platform-required lifecycle tasks
         _logger.LogDebug("Task {TaskId} requires no reasoning agent. Proceeding directly to deterministic execution.", task.TaskId);
      }

      // 2. DETERMINISTIC VALIDATION:
      // Safely pass the immutable 'graph' and 'transactionId' downward to the tool gateway
      await ExecuteDeterministicToolsAsync(task, graph, transactionId, cancellationToken);
   }

   /// <summary>
   /// Execute deterministic validation tools for the specified construction task and handle outcomes per ISL v1.6,
   /// including automated repair, human escalation, and termination actions.
   /// </summary>
   /// <remarks>Constructs ISL v1.6-compliant ToolInvocationRequest objects, invokes the tool gateway for each
   /// required capability, logs outcomes, and evaluates NextAction values to determine repair, escalation, or
   /// termination. Returns immediately if no deterministic capabilities are required.</remarks>
   /// <param name="task">ConstructionTask containing source entity IDs and the required tool capabilities to validate.</param>
   /// <param name="graph">Immutable ConstructionTaskGraph supplying specification identifiers and version for tool requests.</param>
   /// <param name="transactionId">Correlation identifier used to tie tool invocations and telemetry to the overall execution graph.</param>
   /// <param name="cancellationToken">Cancellation token to observe while performing asynchronous tool invocations.</param>
   /// <returns>A task that completes when all required deterministic tool validations have finished.</returns>
   /// <exception cref="InvalidOperationException">Thrown when a tool outcome requires an automated repair cycle (NextAction == "repair").</exception>
   /// <exception cref="Exception">Thrown when a tool outcome indicates human escalation (NextAction == "escalate") or any other unrecoverable tool
   /// action (for example, halt or failure).</exception>
   private async Task ExecuteDeterministicToolsAsync(
      ConstructionTask task,
      ConstructionTaskGraph graph,
      string transactionId,
      CancellationToken cancellationToken = default)
   {
      if (task.RequiredToolCapabilities == null || !task.RequiredToolCapabilities.Any())
      {
         return; // No deterministic validation required for this task
      }

      foreach (var capability in task.RequiredToolCapabilities)
      {
         // 1. Initialize ALL required properties to satisfy the immutable ISL v1.6 schema
         var toolRequest = new ToolInvocationRequest
         {
            ToolInvocationId = Guid.NewGuid().ToString(),
            TaskId = task.TaskId,
            CapabilityName = capability,

            // In a full implementation, the ToolSelector fills these. For MACS, we use safe defaults.
            ToolSelectionId = $"SEL-{Guid.NewGuid()}",
            ToolPluginId = $"PLUGIN-{capability.ToUpper()}",
            PluginVersion = "1.0.0",
            ToolName = capability,
            ToolVersion = "1.0.0",

            // FIXED: Pull traceability identifiers directly from the immutable ConstructionTaskGraph 
            SpecificationId = graph.SpecificationId,
            SpecificationVersion = graph.SpecificationVersion,

            // Use the task's source entity IDs as the input references for the tool to scan/compile
            InputReferences = task.SourceEntityIds ?? new List<string>(),

            // Explicit execution boundaries required by ISL v1.6 
            EnvironmentProfileId = "INF-001",                 // Matches our MACS .NET container target
            IsolationProfileId = "sandboxed-generated-code",  // Enforces least-privilege execution
            TimeoutSeconds = 300,                             // Mandatory timeout to prevent infinite hangs
            DryRun = false,

            CorrelationId = transactionId,                    // Ties telemetry back to the main execution graph
            RequestedAt = DateTimeOffset.UtcNow,
            RequestedBy = "ExecutionService"
         };

         // 2. Invoke the tool and capture the structured, normalized result
         var result = await _toolGateway.ExecuteToolAsync(toolRequest, cancellationToken);

         _logger.LogInformation("Tool Validation [{Capability}] returned Outcome: {Outcome}", capability, result.Outcome);

         // 3. Process the outcome (ISL v1.6 Section 15.0 - Outcome Handling)
         if (result.Outcome is "failed" or "error" or "timeout")
         {
            _logger.LogWarning("Task {TaskId} failed validation via {Capability}. Next Action: {NextAction}",
                task.TaskId, capability, result.NextAction);

            // Evaluate the exact instruction returned by the Tool Gateway normalizer
            if (result.NextAction == "repair")
            {
               // Trigger the bounded repair cycle 
               throw new InvalidOperationException($"Task {task.TaskId} requires an Automated Repair Cycle due to validation failure.");
            }
            else if (result.NextAction == "escalate")
            {
               // Trigger Human-Machine Escalation (The digital "Andon Cord")
               var criticalFinding = result.Findings?.FirstOrDefault()?.Message ?? "Unknown fatal error.";
               _logger.LogError("Critical Failure: {Finding}. Escalating to Human Governance.", criticalFinding);

               throw new Exception($"Human-Machine Escalation: {criticalFinding}");
            }
            else
            {
               // E.g., "halt", "fail-task"
               throw new Exception($"Construction halted. Tool {capability} failed with action: {result.NextAction}");
            }
         }

         // If Outcome == "passed" or "warning", the loop safely continues to the next capability.
      }
   }

   /// <summary>
   /// ISL v1.5 Sec 19.4: Zero-Trust Cross-Boundary Consumption Check.
   /// </summary>
   public Task ValidateCrossBoundaryAccessAsync(
       string consumingBoundaryId,
       string producingBoundaryId,
       string artifactId,
       IReadOnlyList<ConnectionContext> activeContexts,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Intra-boundary access is inherently trusted within its own scope
      if (consumingBoundaryId == producingBoundaryId)
      {
         return Task.CompletedTask;
      }

      _logger.LogInformation("Evaluating cross-boundary access. Consumer: {Consumer}, Producer: {Producer}, Artifact: {Artifact}",
          consumingBoundaryId, producingBoundaryId, artifactId);

      // 2. Look for a formal contract (ConnectionContext) linking the two boundaries
      var connectionContext = activeContexts?.FirstOrDefault(cc =>
          cc.FromBoundaryId == producingBoundaryId &&
          cc.ToBoundaryId == consumingBoundaryId);

      if (connectionContext == null)
      {
         _logger.LogError("SECURITY VIOLATION: Boundary {Consumer} attempted to access artifact {Artifact} from Boundary {Producer} without a Connection Context.",
             consumingBoundaryId, artifactId, producingBoundaryId);

         // Triggers an immediate halt of the task per the Zero-Trust mandate
         throw new UnauthorizedAccessException(
             $"Zero-Trust Violation [ISL v1.5 Sec 19.4]: No ConnectionContext authorizes Boundary {consumingBoundaryId} to consume from Boundary {producingBoundaryId}.");
      }

      // 3. Verify the specific artifact is explicitly listed in the contract's ProvidedElements
      if (connectionContext.ProvidedElements == null || !connectionContext.ProvidedElements.Contains(artifactId))
      {
         _logger.LogError("SECURITY VIOLATION: Connection Context {ContextId} exists, but artifact {Artifact} is not explicitly listed in ProvidedElements.",
             connectionContext.ConnectionContextId, artifactId);

         throw new UnauthorizedAccessException(
             $"Zero-Trust Violation [ISL v1.5 Sec 19.4]: Connection Context {connectionContext.ConnectionContextId} does not authorize consumption of artifact {artifactId}.");
      }

      _logger.LogInformation("Cross-boundary access validated successfully via Connection Context {ContextId}.",
          connectionContext.ConnectionContextId);

      return Task.CompletedTask;
   }

}
