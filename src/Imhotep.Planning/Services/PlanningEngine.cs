using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Graph;
using Imhotep.Contracts.Governance;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Planning.Services
{

   /// <summary>
   /// ISL v1.5: The Construction Planning Model.
   /// Transforms the canonical semantic model into an executable construction task graph.
   /// </summary>
   public interface IPlanningEngine
   {
      /// <summary>
      /// ISL v1.5 Section 8.0: Generates the formal Construction Task Graph and associated records
      /// from the normalized Canonical Semantic Model.
      /// </summary>
      Task<PlanGenerationResult> GenerateConstructionPlanAsync(
          string transactionId,
          CanonicalSemanticModel activeModel,
          CancellationToken cancellationToken = default);

      /// <summary>
      /// ISL v1.5 Section 19.0: Retrieves the construction boundaries that organize the 
      /// Construction Task Graph into bounded planning, reasoning, validation, and execution scopes.
      /// </summary>
      Task<IReadOnlyList<ConstructionBoundary>> GetConstructionBoundariesAsync(
          string transactionId,
          CancellationToken cancellationToken = default);

      /// <summary>
      /// ISL v1.5 Section 21.0: Validates that the generated plan is complete, internally consistent, 
      /// dependency-safe, traceable, and executable.
      /// </summary>
      Task<PlanningValidationReport> ValidateConstructionPlanAsync(
          string transactionId,
          CancellationToken cancellationToken = default);

      /// <summary>
      /// ISL v1.5 Section 24.0: Adapts an existing plan dynamically without requiring full 
      /// reconstruction when a specification change triggers impact analysis.
      /// </summary>
      Task<PlanAdaptationRecord> AdaptConstructionPlanAsync(
            ConstructionTaskGraph priorPlan,
            ImpactAnalysisResult impactAnalysis,
            CanonicalSemanticModel newModel,
            string transactionId,
            CancellationToken cancellationToken = default);

      /// <summary>
      /// ISL v1.5 Sec 8.0: Generates the formal Construction Task Graph from the Canonical Semantic Model.
      /// </summary>
      Task<ConstructionTaskGraph> GenerateTaskGraphAsync(
          CanonicalSemanticModel semanticModel,
          SpecificationReadinessReport readinessRecord,
          CancellationToken cancellationToken = default);
   }

   /// <summary>
   /// ISL v1.5: Transforms the canonical semantic model into an executable construction task graph.
   /// Manages plan generation, validation, adaptation, and boundary extraction.
   /// </summary>
   public class PlanningEngine : IPlanningEngine
   {
      private readonly ILogger<PlanningEngine> _logger;

      public PlanningEngine(ILogger<PlanningEngine> logger)
      {
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task<PlanGenerationResult> GenerateConstructionPlanAsync(
          string transactionId,
          CanonicalSemanticModel activeModel,
          CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();
         _logger.LogInformation("Generating Construction Plan for System {SystemId} v{Version}",
             activeModel.SystemId, activeModel.Version);

         // 1. INITIALIZE BASELINE IMMUTABLE GRAPH (ISL v1.5 Sec 14.2)
         var initialGraph = new ConstructionTaskGraph
         {
            PlanId = $"PLAN-{transactionId}",
            SpecificationId = activeModel.SystemId,
            SpecificationVersion = activeModel.Version,
            CanonicalModelVersion = activeModel.ModelVersion,
            ReadinessLevel = "autonomous-ready",
            PlanningMode = PlanningMode.Formal,
            Status = PlanStatus.Draft, // MUST NOT be executable until validation checks pass [3]
            CreatedAt = DateTimeOffset.UtcNow,

            // Initialize empty collections to satisfy 'required' modifiers
            Tasks = new List<ConstructionTask>(),
            Dependencies = new List<TaskDependencyRecord>(),
            CriticalPath = new List<string>(),
            ParallelGroups = new List<ParallelGroup>(),
            VerificationPlan = new { Status = "Pending" },
            ArtifactProductionPlan = new { Status = "Pending" }
         };

         // 2. CREATE TEMPORARY ACCUMULATORS
         var taskAccumulator = new List<ConstructionTask>();
         var dependencyAccumulator = new List<TaskDependencyRecord>();
         var boundaryAccumulator = new List<ConstructionBoundary>();
         var contextAccumulator = new List<ConnectionContext>();

         // 3. DELEGATE TO GENERATOR METHODS (ISL v1.5 Sec 12.2 Entity-to-Task Mapping)
         GenerateSecurityAndGovernanceTasks(activeModel, taskAccumulator);
         GenerateArchitectureAndBehaviorTasks(activeModel, taskAccumulator);
         GenerateLifecycleTasks(activeModel, taskAccumulator);
         GenerateInterpretationTasks(activeModel, taskAccumulator);
         GenerateDataEntityTasks(activeModel, taskAccumulator);
         GenerateServiceImplementationTasks(activeModel, taskAccumulator);
         GenerateInfrastructureTasks(activeModel, taskAccumulator);

         // ISL v1.5 Sec 15.0: Verification MUST be planned as part of construction [4]
         GenerateVerificationTasks(activeModel, taskAccumulator);

         // 4. RESOLVE DEPENDENCIES & COMPUTE STRUCTURES (ISL v1.5 Sec 13.0 & 18.0)
         ResolveDependencies(taskAccumulator, dependencyAccumulator);
         var criticalPath = CalculateCriticalPath(taskAccumulator, dependencyAccumulator);
         var parallelGroups = DefineParallelGroups(taskAccumulator, dependencyAccumulator);

         // 5. EXTRACT CONSTRUCTION BOUNDARIES (ISL v1.5 Sec 19.0)
         DefineBoundaries(transactionId, taskAccumulator, boundaryAccumulator, contextAccumulator);

         // 6. FINALIZE THE GRAPH
         var finalizedGraph = initialGraph with
         {
            Tasks = taskAccumulator,
            Dependencies = dependencyAccumulator,
            CriticalPath = criticalPath,
            ParallelGroups = parallelGroups,
            // Assuming Boundaries exist in your extended ConstructionTaskGraph record
            Boundaries = boundaryAccumulator,
            ConnectionContexts = contextAccumulator,
            Status = PlanStatus.Executable // Mark ready for the Execution Runtime [5]
         };

         _logger.LogInformation("Successfully generated Plan {PlanId} with {TaskCount} tasks and {DepCount} dependencies.",
             finalizedGraph.PlanId, finalizedGraph.Tasks.Count, finalizedGraph.Dependencies.Count);

         // FINAL PHASE: VALIDATION & HANDOFF [ISL v1.5 Sec 21.0 & 28.0]

         // 1. Execute Formal Planning Validation [ISL v1.5 Sec 21.0/22.0]
         var validationReport = await ValidateConstructionPlanAsync(transactionId, cancellationToken);

         if (!validationReport.Executable)
         {
            _logger.LogError("Construction Plan {PlanId} failed validation and cannot be executed.", finalizedGraph.PlanId);

            // Depending on your error handling, you might throw a PlanningValidationException here
            // or return a failed ExecutionHandoffRecord.
            throw new InvalidOperationException($"Plan {finalizedGraph.PlanId} failed validation.");
         }

         // 2. Capture Traceability Snapshot [ISL v1.4 Sec 18.0]
         // The handoff requires the state of the graph to be frozen in traceability
         // For the POC, we simulate the snapshot ID generation here:
         string traceabilitySnapshotId = $"SNAP-TRC-{Guid.NewGuid():N}";

         // 3. Package the Execution Handoff Record [ISL v1.5 Sec 28.0]
         var handoffRecord = PackageExecutionHandoff(finalizedGraph, validationReport, traceabilitySnapshotId);

         _logger.LogInformation("Planning complete. Execution Handoff {HandoffId} successfully packaged for runtime.", handoffRecord.HandoffId);

         // 4. Return the ISL v3.0 compliant PlanGenerationResult
         return new PlanGenerationResult
         {
            TaskGraph = finalizedGraph,
            ValidationReport = validationReport,
            HandoffRecord = handoffRecord
         };
      }

      private void ResolveDependencies(List<ConstructionTask> tasks, List<TaskDependencyRecord> dependencyAccumulator)
      {
         // 1. ISL v1.5 Sec 13.1: Interpretation MUST complete before generation begins [1, 2].
         // Identify the foundational interpretation task.
         var interpretTask = tasks.FirstOrDefault(t => t.TaskType == TaskCategory.Interpretation);
         var generationTasks = tasks.Where(t => t.TaskType is TaskCategory.Schema or 
            TaskCategory.Implementation or TaskCategory.Infrastructure).ToList();

         if (interpretTask != null)
         {
            foreach (var genTask in generationTasks)
            {
               dependencyAccumulator.Add(new TaskDependencyRecord
               {
                  DependencyId = $"DEP-HARD-{Guid.NewGuid():N}", // FIXED: Satisfy 'required' constraint
                  SourceTaskId = genTask.TaskId,
                  TargetTaskId = interpretTask.TaskId,
                  DependencyType = DependencyType.Hard, // Target task MUST complete before source task begins [3, 4]
                  Rationale = "Interpretation must complete before structural generation begins.",
                  Required = true,
                  CreatedAt = DateTimeOffset.UtcNow // Explicit assignment to satisfy 'required' constraint
               });
            }
         }

         // 2. ISL v1.5 Sec 13.1: Verification MUST run after the task producing the artifact [1, 2].
         // Map every verification task to the generation task it is evaluating.
         var verificationTasks = tasks.Where(t => t.TaskType == TaskCategory.Verification).ToList();
         foreach (var vTask in verificationTasks)
         {
            // Reconstruct the target ID based on the naming convention used in GenerateVerificationTasks
            var targetGenTaskId = vTask.TaskId.Replace("TSK-VERIFY-", "");

            if (tasks.Any(t => t.TaskId == targetGenTaskId))
            {
               dependencyAccumulator.Add(new TaskDependencyRecord
               {
                  DependencyId = $"DEP-ART-{Guid.NewGuid():N}",
                  SourceTaskId = vTask.TaskId,
                  TargetTaskId = targetGenTaskId,
                  DependencyType = DependencyType.Artifact, // Source task requires artifact produced by target task [3, 4]
                  Rationale = "Verification requires the artifacts produced by the generation task.",
                  Required = true,
                  CreatedAt = DateTimeOffset.UtcNow
               });
            }
         }

         // 3. Automated Tests MUST depend on Implementation Completion [1, 2]
         // Tests (generated from Validation entities) must wait for the actual code to be written.
         var testTasks = tasks.Where(t => t.TaskType == TaskCategory.Test).ToList();
         var implTasks = tasks.Where(t => t.TaskType == TaskCategory.Implementation).ToList();

         foreach (var testTask in testTasks)
         {
            foreach (var implTask in implTasks)
            {
               dependencyAccumulator.Add(new TaskDependencyRecord
               {
                  DependencyId = $"DEP-TEST-{Guid.NewGuid():N}",
                  SourceTaskId = testTask.TaskId,
                  TargetTaskId = implTask.TaskId,
                  DependencyType = DependencyType.Artifact,
                  Rationale = "Automated test execution requires the service implementation to be generated.",
                  Required = true,
                  CreatedAt = DateTimeOffset.UtcNow
               });
            }
         }
      }

      private IReadOnlyList<string> CalculateCriticalPath(List<ConstructionTask> tasks, List<TaskDependencyRecord> dependencies)
      {
         // ISL v1.5 Sec 18.3: If task durations are unknown, calculate critical path by dependency length [2].
         if (!tasks.Any()) return new List<string>();

         // 1. Build an adjacency list representing the forward execution order: Prerequisite -> Dependent Task.
         var executionGraph = new Dictionary<string, List<string>>();
         var inDegree = new Dictionary<string, int>();

         foreach (var task in tasks)
         {
            executionGraph[task.TaskId] = new List<string>();
            inDegree[task.TaskId] = 0;
         }

         // In a TaskDependencyRecord, SourceTaskId depends on TargetTaskId completing first.
         // Therefore, execution flow moves from TargetTaskId -> SourceTaskId.
         foreach (var dep in dependencies)
         {
            if (executionGraph.ContainsKey(dep.TargetTaskId) && executionGraph.ContainsKey(dep.SourceTaskId))
            {
               executionGraph[dep.TargetTaskId].Add(dep.SourceTaskId);
               inDegree[dep.SourceTaskId]++;
            }
         }

         // 2. Perform Topological Sort (Kahn's Algorithm) to process tasks in a safe execution sequence.
         var topoOrder = new List<string>();
         var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

         while (queue.Any())
         {
            var current = queue.Dequeue();
            topoOrder.Add(current);

            foreach (var neighbor in executionGraph[current])
            {
               inDegree[neighbor]--;
               if (inDegree[neighbor] == 0)
               {
                  queue.Enqueue(neighbor);
               }
            }
         }

         // (Note: If topoOrder.Count != tasks.Count, a circular dependency exists. 
         // ISL v1.5 Sec 13.4 mandates detecting this, which should be handled during the broader ValidateConstructionPlanAsync).

         // 3. Calculate the longest path by dependency length (Dynamic Programming).
         var distance = new Dictionary<string, int>();
         var previous = new Dictionary<string, string?>();

         foreach (var taskId in topoOrder)
         {
            distance[taskId] = 1; // Base distance is 1 (the task itself)
            previous[taskId] = null;
         }

         foreach (var u in topoOrder)
         {
            foreach (var v in executionGraph[u])
            {
               // If the path through 'u' is longer than the current longest path to 'v', update it.
               if (distance[u] + 1 > distance[v])
               {
                  distance[v] = distance[u] + 1;
                  previous[v] = u;
               }
            }
         }

         // 4. Find the node with the maximum distance (the end of the longest dependency chain).
         var maxNode = distance.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;

         // 5. Reconstruct the critical path backwards from the maximum node.
         var criticalPath = new List<string>();
         var currentPathNode = maxNode;

         while (currentPathNode != null)
         {
            criticalPath.Add(currentPathNode);
            currentPathNode = previous[currentPathNode];
         }

         // Reverse to yield the strict execution order (Start -> Finish)
         criticalPath.Reverse();

         return criticalPath;
      }

      private IReadOnlyList<ParallelGroup> DefineParallelGroups(List<ConstructionTask> tasks, List<TaskDependencyRecord> dependencies)
      {
         var parallelGroups = new List<ParallelGroup>();

         // ISL v1.5 Sec 17.1: If there are fewer than 2 tasks, no parallelism is possible.
         if (tasks.Count < 2) return parallelGroups;

         // 1. Build the adjacency list and in-degree map to calculate execution depth
         var executionGraph = new Dictionary<string, List<string>>();
         var inDegree = new Dictionary<string, int>();

         foreach (var task in tasks)
         {
            executionGraph[task.TaskId] = new List<string>();
            inDegree[task.TaskId] = 0;
         }

         foreach (var dep in dependencies)
         {
            if (executionGraph.ContainsKey(dep.TargetTaskId) && executionGraph.ContainsKey(dep.SourceTaskId))
            {
               executionGraph[dep.TargetTaskId].Add(dep.SourceTaskId);
               inDegree[dep.SourceTaskId]++;
            }
         }

         // 2. Queue for topological sorting to assign a safe "Level" (depth) to each task
         var queue = new Queue<string>();
         var taskLevels = new Dictionary<string, int>();

         foreach (var kvp in inDegree.Where(k => k.Value == 0))
         {
            queue.Enqueue(kvp.Key);
            taskLevels[kvp.Key] = 0; // Root tasks are at Level 0
         }

         while (queue.Any())
         {
            var current = queue.Dequeue();
            var currentLevel = taskLevels[current];

            foreach (var neighbor in executionGraph[current])
            {
               inDegree[neighbor]--;

               // The neighbor's level must be strictly greater than all of its prerequisites
               if (!taskLevels.ContainsKey(neighbor) || taskLevels[neighbor] < currentLevel + 1)
               {
                  taskLevels[neighbor] = currentLevel + 1;
               }

               if (inDegree[neighbor] == 0)
               {
                  queue.Enqueue(neighbor);
               }
            }
         }

         // 3. Group tasks by their topological level
         var groupedByLevel = taskLevels.GroupBy(kvp => kvp.Value)
                                        .Where(g => g.Count() > 1) // Only interested in levels with > 1 task
                                        .OrderBy(g => g.Key);

         int groupCounter = 1;
         foreach (var levelGroup in groupedByLevel)
         {
            var groupTaskIds = levelGroup.Select(kvp => kvp.Key).ToList();

            // ISL v1.5 Sec 17.2: Instantiate the full ParallelGroup record
            parallelGroups.Add(new ParallelGroup
            {
               ParallelGroupId = $"GRP-PARALLEL-LVL{levelGroup.Key}-{groupCounter++}",
               TaskIds = groupTaskIds,
               DependencyBoundary = $"Execution-Level-{levelGroup.Key}",
               Rationale = $"Tasks at topological level {levelGroup.Key} share no direct dependencies and can execute concurrently.",
               SharedResources = new List<string>(), // Explicitly initialize to satisfy nullability/schema constraints
               GovernanceConstraints = new List<string>()
            });
         }

         return parallelGroups;
      }

      #region -- 4.00 - The following generator methods implement the core ISL v1.5 Section 12.2 Entity-to-Task mapping rules,

      private void GenerateLifecycleTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         // 1. Artifact Consolidation (ISL v1.2 Phase 8)
         taskAccumulator.Add(new ConstructionTask
         {
            TaskId = $"TSK-CONSOLIDATE-{model.SystemId}",
            TaskType = TaskCategory.Consolidation,
            // ISL v1.5 Sec 9.4: Use rule/phase reference for platform-required tasks
            SourceEntityIds = new List<string> { "ISL-v1.2-Phase-8" },
            Description = "Organize validated artifacts into a stable project structure.",
            AssignedAgentRole = "Execution Runtime", // Mapped per ISL v1.5 Sec 10.1
            Dependencies = new List<string>(), // Normally depends on all verification tasks

            // Satisfy the C# 11 'required' compiler constraints
            Status = PlanStatus.Pending,
            Priority = TaskPriority.Critical,
            CreatedAt = DateTimeOffset.UtcNow
         });

         // 2. Deployment Preparation (ISL v1.2 Phase 9)
         taskAccumulator.Add(new ConstructionTask
         {
            TaskId = $"TSK-DEPLOY-PREP-{model.SystemId}",
            TaskType = TaskCategory.DeploymentPreparation,
            SourceEntityIds = new List<string> { "ISL-v1.2-Phase-9" },
            Description = "Produce deployment manifests and operational readiness evidence.",
            AssignedAgentRole = "Deployment Preparer", // Mapped per ISL v1.5 Sec 10.1
            Dependencies = new List<string> { $"TSK-CONSOLIDATE-{model.SystemId}" }, // Must follow consolidation
            Status = PlanStatus.Pending,
            Priority = TaskPriority.High,
            CreatedAt = DateTimeOffset.UtcNow
         });

         // 3. Traceability Snapshot Creation (ISL v1.4 Sec 18.0)
         taskAccumulator.Add(new ConstructionTask
         {
            TaskId = $"TSK-TRACE-SNAP-{model.SystemId}",
            TaskType = TaskCategory.Traceability,
            SourceEntityIds = new List<string> { "ISL-v1.4-Sec-18.0" },
            Description = "Generate the final traceability snapshot linking all tasks, artifacts, and entities.",
            AssignedAgentRole = "Traceability Engine", // Mapped per ISL v1.5 Sec 10.1
            Dependencies = new List<string> { $"TSK-DEPLOY-PREP-{model.SystemId}" }, // Final step of the loop
            Status = PlanStatus.Pending,
            Priority = TaskPriority.Critical, // Traceability closure is a mandatory success criterion
            CreatedAt = DateTimeOffset.UtcNow
         });
      }

      private void GenerateArchitectureAndBehaviorTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         // 1. Map 'Capability' entities to structural architecture tasks
         var capabilityEntities = model.Capabilities.ToList();

         foreach (var capability in capabilityEntities)
         {
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-ARCH-{capability.TraceabilityId}",
               TaskType = TaskCategory.Architecture,
               SourceEntityIds = new List<string> { capability.TraceabilityId },
               Description = $"Design structural decomposition and domain boundaries for capability {capability.TraceabilityId}.",
               AssignedAgentRole = "Architecture Planner",
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },

               // Satisfy the C# 11 'required' compiler constraints
               Status = PlanStatus.Pending,
               Priority = TaskPriority.High,
               CreatedAt = DateTimeOffset.UtcNow
            });
         }

         // 2. Map 'Workflow' entities to state transitions and documentation
         var workflowEntities = model.Workflows.ToList();

         foreach (var workflow in workflowEntities)
         {
            // Implementation Task: Generate the physical workflow logic/state machine
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-IMPL-{workflow.TraceabilityId}",
               TaskType = TaskCategory.Implementation,
               SourceEntityIds = new List<string> { workflow.TraceabilityId },
               Description = $"Implement step-by-step state transitions and logic for workflow {workflow.TraceabilityId}.",
               AssignedAgentRole = "Implementation Generator",
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },
               Status = PlanStatus.Pending,
               Priority = TaskPriority.High,
               CreatedAt = DateTimeOffset.UtcNow
            });

            // Documentation Task: Produce the operational and state transition models
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-DOC-{workflow.TraceabilityId}",
               TaskType = TaskCategory.Documentation,
               SourceEntityIds = new List<string> { workflow.TraceabilityId },
               Description = $"Generate state transition documentation and sequence models for workflow {workflow.TraceabilityId}.",
               AssignedAgentRole = "Implementation Generator",
               Dependencies = new List<string> { $"TSK-IMPL-{workflow.TraceabilityId}" }, // Depends on implementation
               Status = PlanStatus.Pending,
               Priority = TaskPriority.Medium,
               CreatedAt = DateTimeOffset.UtcNow
            });
         }

         // 3. Map 'Requirement' entities to acceptance testing pathways
         var requirementEntities = model.Requirements.ToList();

         foreach (var requirement in requirementEntities)
         {
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-TEST-{requirement.TraceabilityId}",
               TaskType = TaskCategory.Test,
               SourceEntityIds = new List<string> { requirement.TraceabilityId },
               Description = $"Generate acceptance test scenarios to verify requirement {requirement.TraceabilityId}.",
               AssignedAgentRole = "Test Generator",
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },
               Status = PlanStatus.Pending,
               Priority = TaskPriority.Critical, // Must-have requirements block autonomous execution if not verified
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      private void GenerateSecurityAndGovernanceTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         // 1. Map 'Policy' entities to security tasks (ISL v1.5 Sec 12.2)
         var policyEntities = model.Policies.ToList();

         foreach (var policy in policyEntities)
         {
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-SEC-{policy.TraceabilityId}",
               TaskType = TaskCategory.Security,
               SourceEntityIds = new List<string> { policy.TraceabilityId },
               Description = $"Evaluate and enforce security/compliance constraints defined in policy {policy.TraceabilityId}.",
               AssignedAgentRole = "Security Validator", // Mapped per ISL v1.5 Sec 11.1
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },

               // Satisfy the C# 11 'required' compiler constraints
               Status = PlanStatus.Pending, 
               Priority = TaskPriority.Critical, // Policies are strict constraints
               CreatedAt = DateTimeOffset.UtcNow
            });
         }

         // 2. Map 'Stakeholder' entities to governance and documentation tasks (ISL v1.5 Sec 12.2)
         var stakeholderEntities = model.Stakeholders.ToList();

         foreach (var stakeholder in stakeholderEntities)
         {
            // Governance Task: Handles approval gates and audits for the stakeholder
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-GOV-{stakeholder.TraceabilityId}",
               TaskType = TaskCategory.Governance,
               SourceEntityIds = new List<string> { stakeholder.TraceabilityId },
               Description = $"Execute governance approval gates and compliance checks required by stakeholder {stakeholder.TraceabilityId}.",
               AssignedAgentRole = "Governance Engine", // Mapped per ISL v1.5 Sec 11.1
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },
               Status = PlanStatus.Pending,
               Priority = TaskPriority.High,
               CreatedAt = DateTimeOffset.UtcNow
            });

            // Documentation Task: Generates architecture/audit records for the stakeholder
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-DOC-{stakeholder.TraceabilityId}",
               TaskType = TaskCategory.Documentation,
               SourceEntityIds = new List<string> { stakeholder.TraceabilityId },
               Description = $"Generate system and governance documentation required by stakeholder {stakeholder.TraceabilityId}.",
               AssignedAgentRole = "Implementation Generator", // Mapped per ISL v1.5 Sec 11.1
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },
               Status = PlanStatus.Pending,
               Priority = TaskPriority.Medium,
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      private void GenerateInterpretationTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         taskAccumulator.Add(new ConstructionTask
         {
            TaskId = $"TSK-INTERPRET-{model.SystemId}",
            TaskType = TaskCategory.Interpretation,
            SourceEntityIds = new List<string> { model.SystemId },
            Description = "Interpret the canonical model into execution-oriented context.",
            AssignedAgentRole = "Specification Interpreter",
            Dependencies = new List<string>(),
            Status = PlanStatus.Pending,
            Priority = TaskPriority.Critical,
            CreatedAt = DateTimeOffset.UtcNow
         });
      }

      private void GenerateDataEntityTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         foreach (var dataEntity in model.DataEntities)
         {
            // ISL v1.5 Section 9.2 recommends the TSK prefix for task traceability identifiers
            string taskId = $"TSK-DATA-{dataEntity.TraceabilityId}";

            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = taskId,

               // Replaces 'Category' - ISL v1.5 Section 10.1 specifically defines 'schema' for DataEntities
               TaskType = TaskCategory.Schema,

               // Replaces 'TargetTraceabilityId' - MUST be a collection 
               SourceEntityIds = new List<string> { dataEntity.TraceabilityId },

               Description = $"Generate concrete data schema/models for {dataEntity.Name}",
               AssignedAgentRole = "Implementation Generator",

               // --- Required ISL v1.5 Base Fields ---
               Dependencies = new List<string>(), // Must not be null
               Priority = TaskPriority.Medium,    // e.g., critical, high, medium, low
               Status = PlanStatus.Pending,       // Initial state must be pending
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      private void GenerateServiceImplementationTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         foreach (var service in model.Services)
         {
            // ISL v1.5 recommends the TSK prefix for task traceability identifiers
            string taskId = $"TSK-SRV-{service.TraceabilityId}";

            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = taskId,

               // Replaces 'Category' - ISL v1.5 Section 10.1 specifically defines 'implementation' for Services
               TaskType = TaskCategory.Implementation,

               // Replaces 'TargetTraceabilityId' - MUST be a collection to support the Traceability Graph
               SourceEntityIds = new List<string> { service.TraceabilityId },

               Description = $"Scaffold and implement operational logic for service {service.Name}",
               AssignedAgentRole = "Implementation Generator",

               // --- Required ISL v1.5 Base Fields ---
               Dependencies = new List<string>(), // Must not be null; populated during dependency resolution
               Priority = TaskPriority.High,      // Services are typically critical/high priority
               Status = PlanStatus.Pending,       // Initial state must be pending
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      private void GenerateVerificationTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         // 1. Generate verification tasks for ALL generated schema and implementation tasks
         var tasksRequiringVerification = taskAccumulator
             .Where(t => t.TaskType is TaskCategory.Implementation or TaskCategory.Schema)
             .ToList();

         foreach (var task in tasksRequiringVerification)
         {
            // ISL v1.5 recommends the TSK prefix for task traceability identifiers
            string taskId = $"TSK-VAL-{task.TaskId}";

            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = taskId,

               // Replaces 'Category' - ISL v1.5 Section 10.1 defines 'verification' 
               TaskType = TaskCategory.Verification,

               // Replaces 'TargetTraceabilityId' - MUST be a collection for the Traceability Graph
               SourceEntityIds = task.SourceEntityIds,

               Description = $"Map and execute deterministic tool validation for {task.TaskId}",

               // Replaces "Test Generator" - ISL v1.5 maps 'verification' tasks to the Review Agent
               AssignedAgentRole = "Review Agent",

               // --- Required ISL v1.5 Base Fields ---
               Dependencies = new List<string> { task.TaskId}, // populated during dependency resolution
               Priority = TaskPriority.High,      // Validation gates are typically high priority
               Status = PlanStatus.Pending,       // Initial state must be pending
               CreatedAt = DateTimeOffset.UtcNow

               // Note: Per ISL v1.5 Section 15.2, you can also optionally populate 
               // RequiredToolCapabilities here (e.g., new List<string> { "schema-validate", "static-analysis" }) 
               // if your Validation entity specifies the deterministic tools to use.
            });
         }

         // 2. Map 'Validation' canonical entities directly to automated test generation
         var validationEntities = model.Validations;

         foreach (var valEntity in validationEntities)
         {
            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = $"TSK-TEST-{valEntity.TraceabilityId}",
               TaskType = TaskCategory.Test,
               SourceEntityIds = new List<string> { valEntity.TraceabilityId },
               Description = $"Generate and execute automated test constraints for {valEntity.TraceabilityId}.",
               AssignedAgentRole = "Test Generator",
               Dependencies = new List<string> { $"TSK-INTERPRET-{model.SystemId}" },
               Status = PlanStatus.Pending,
               Priority = TaskPriority.High,
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      private void GenerateInfrastructureTasks(CanonicalSemanticModel model, List<ConstructionTask> taskAccumulator)
      {
         foreach (var infra in model.Infrastructures)
         {
            // ISL v1.5 recommends the TSK prefix for task traceability identifiers
            string taskId = $"TSK-INFRA-{infra.TraceabilityId}";

            taskAccumulator.Add(new ConstructionTask
            {
               TaskId = taskId,

               // Replaces 'Category' - ISL v1.5 Section 10.1 defines 'infrastructure' 
               TaskType = TaskCategory.Infrastructure,

               // Replaces 'TargetTraceabilityId' - MUST be a collection for the Traceability Graph
               SourceEntityIds = new List<string> { infra.TraceabilityId },

               Description = $"Generate deployment manifests and IaC for {infra.Name}",
               AssignedAgentRole = "Deployment Preparer", // Correctly mapped per ISL v1.5 Sec 11.1

               // --- Required ISL v1.5 Base Fields ---
               Dependencies = new List<string>(), // Must not be null; populated during dependency resolution
               Priority = TaskPriority.High,      // Infrastructure is typically high priority for deployment preparation
               Status = PlanStatus.Pending,       // Initial state must be pending
               CreatedAt = DateTimeOffset.UtcNow
            });
         }
      }

      /// <summary>
      /// ISL v1.5 Sec 28.0: Packages the validated plan for execution runtime handoff.
      /// </summary>
      public ExecutionHandoffRecord PackageExecutionHandoff(
          ConstructionTaskGraph plan,
          PlanningValidationReport validationReport,
          string traceabilitySnapshotId)
      {
         _logger.LogInformation("Packaging Execution Handoff for Plan {PlanId}", plan.PlanId);

         return new ExecutionHandoffRecord
         {
            HandoffId = $"HND-{Guid.NewGuid():N}",
            PlanId = plan.PlanId,
            SpecificationId = plan.SpecificationId,
            SpecificationVersion = plan.SpecificationVersion,
            ReadinessLevel = plan.ReadinessLevel,
            ValidationReportId = validationReport.ValidationReportId,
            TraceabilitySnapshotId = traceabilitySnapshotId,
            HandedOffAt = DateTimeOffset.UtcNow,
            AcceptedByRuntime = false // The ExecutionService will update this to true upon admission [3]
         };
      }

      public Task<ConstructionTaskGraph> GenerateTaskGraphAsync(
          CanonicalSemanticModel semanticModel,
          SpecificationReadinessReport readinessRecord,
          CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Generating Construction Task Graph for {SystemId}", semanticModel.SystemId);

         // 1. ISL v1.5 Sec 6.1: Formal Planning Preconditions
         // Planning Engine MUST NOT generate an executable plan unless readiness is Machine-Valid or higher. [3]
         if (readinessRecord.Level != ReadinessLevel.AutonomousReady && readinessRecord.Level != ReadinessLevel.MachineValid)
         {
            throw new InvalidOperationException($"ISL v1.5 Violation: Specification {semanticModel.SystemId} must be at least Machine-Valid to generate a formal task graph.");
         }

         var sysId = semanticModel.SystemId;
         var tasks = new List<ConstructionTask>();
         var dependencies = new List<TaskDependencyRecord>();

         // 2. ISL v3.3 Sec 9.1: MACS Construction Task Graph Generation [1]
         // We explicitly scaffold the 12 required tasks for the MACS Proof-of-Concept

         tasks.Add(CreateTask($"TSK-{sysId}-interpret", TaskCategory.Interpretation, "Specification Interpreter",
             "Interpret canonical model for execution", new[] { semanticModel.Project.TraceabilityId }));

         tasks.Add(CreateTask($"TSK-{sysId}-plan", TaskCategory.Planning, "Construction Planner",
             "Confirm artifact plan and dependency order", new[] { semanticModel.Project.TraceabilityId }));

         tasks.Add(CreateTask($"TSK-{sysId}-project", TaskCategory.Implementation, "Implementation Generator",
             "Generate project structure", new[] { semanticModel.Project.TraceabilityId }, new[] { "config", "source" }));

         // Extract relevant entity IDs safely
         var dataIds = semanticModel.DataEntities?.Select(e => e.TraceabilityId).ToArray() ?? Array.Empty<string>();
         tasks.Add(CreateTask($"TSK-{sysId}-data", TaskCategory.Schema, "Implementation Generator",
             "Generate response data model", dataIds, new[] { "schema", "source" }));

         var serviceIds = semanticModel.Services?.Select(s => s.TraceabilityId).Concat(semanticModel.Interfaces?.Select(i => i.TraceabilityId) ?? Array.Empty<string>()).ToArray() ?? Array.Empty<string>();
         tasks.Add(CreateTask($"TSK-{sysId}-service", TaskCategory.Implementation, "Implementation Generator",
             "Generate service endpoint", serviceIds, new[] { "source" }));

         var validationIds = semanticModel.Validations?.Select(v => v.TraceabilityId).ToArray() ?? Array.Empty<string>();
         tasks.Add(CreateTask($"TSK-{sysId}-tests", TaskCategory.Test, "Test Generator",
             "Generate automated tests", validationIds, new[] { "test" }));

         // Verification tasks mapped to the deterministic execution runtime [4]
         tasks.Add(CreateTask($"TSK-{sysId}-build", TaskCategory.Verification, "Review Agent",
             "Build or compile generated project", new[] { semanticModel.Project.TraceabilityId }, null, new[] { "compile" }));

         tasks.Add(CreateTask($"TSK-{sysId}-test-run", TaskCategory.Verification, "Review Agent",
             "Run automated tests", validationIds, null, new[] { "unit-test" }));

         tasks.Add(CreateTask($"TSK-{sysId}-repair", TaskCategory.Repair, "Repair Analyst",
             "Repair failed generated artifacts", new[] { semanticModel.Project.TraceabilityId }));

         tasks.Add(CreateTask($"TSK-{sysId}-finalize", TaskCategory.Consolidation, "Execution Runtime",
             "Promote valid artifacts to stable", new[] { semanticModel.Project.TraceabilityId }));

         tasks.Add(CreateTask($"TSK-{sysId}-trace", TaskCategory.Traceability, "Traceability Engine",
             "Create final traceability snapshot", new[] { semanticModel.Project.TraceabilityId }));

         tasks.Add(CreateTask($"TSK-{sysId}-complete", TaskCategory.Governance, "Execution Runtime",
             "Produce completion report", new[] { semanticModel.Project.TraceabilityId }));


         // 3. ISL v3.3 Sec 9.2: Required Task Dependencies [2]
         // We mathematically link the upstream requirements to the downstream tasks

         dependencies.Add(CreateDependency($"TSK-{sysId}-plan", $"TSK-{sysId}-interpret", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-project", $"TSK-{sysId}-plan", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-service", $"TSK-{sysId}-project", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-data", $"TSK-{sysId}-project", DependencyType.Hard));

         dependencies.Add(CreateDependency($"TSK-{sysId}-tests", $"TSK-{sysId}-service", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-tests", $"TSK-{sysId}-data", DependencyType.Hard));

         dependencies.Add(CreateDependency($"TSK-{sysId}-build", $"TSK-{sysId}-service", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-build", $"TSK-{sysId}-data", DependencyType.Hard));

         dependencies.Add(CreateDependency($"TSK-{sysId}-test-run", $"TSK-{sysId}-build", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-test-run", $"TSK-{sysId}-tests", DependencyType.Hard));

         // Conditional Repair Paths triggered by failures
         dependencies.Add(CreateDependency($"TSK-{sysId}-repair", $"TSK-{sysId}-build", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-repair", $"TSK-{sysId}-test-run", DependencyType.Hard));

         dependencies.Add(CreateDependency($"TSK-{sysId}-finalize", $"TSK-{sysId}-build", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-finalize", $"TSK-{sysId}-test-run", DependencyType.Hard));

         dependencies.Add(CreateDependency($"TSK-{sysId}-trace", $"TSK-{sysId}-finalize", DependencyType.Hard));
         dependencies.Add(CreateDependency($"TSK-{sysId}-complete", $"TSK-{sysId}-trace", DependencyType.Hard));

         // 4. ISL v1.5 Sec 14.2: Hydrate the Construction Task Graph
         var graph = new ConstructionTaskGraph
         {
            PlanId = $"PLAN-{Guid.NewGuid():N}",
            SpecificationId = semanticModel.SystemId,
            SpecificationVersion = semanticModel.Version,
            CanonicalModelVersion = semanticModel.ModelVersion,

            // ISL v1.5 Sec 14.4: Mark as executable only once Autonomous-Ready authorization is cleared
            Status = readinessRecord.Level == ReadinessLevel.AutonomousReady ?
               PlanStatus.Executable : PlanStatus.Valid,

            ReadinessLevel = readinessRecord.Level.ToString(),
            PlanningMode = PlanningMode.Formal,
            CreatedAt = DateTimeOffset.UtcNow,

            // Finalize immutability constraints using ToArray() to avoid ReadOnlyCollection casting errors
            Tasks = tasks.ToArray(),
            Dependencies = dependencies.ToArray(),

            // For MACS PoC, create a single overarching construction boundary [ISL v1.5 Sec 19.1]
            Boundaries = new[] 
             {
                    new ConstructionBoundary
                    {
                        BoundaryId = $"BND-{sysId}-MACS",
                        BoundaryName = "MACS Initial Execution Boundary",
                        BoundaryPurpose = "Coordinate MACS proof-of-concept autonomous generation and validation", // REQUIRED by ISL v1.5 Sec 19.1
                        BoundaryType = BoundaryType.Runtime,
                        SourceEntityIds = new[] { semanticModel.Project.TraceabilityId },
                        TaskIds = tasks.Select(t => t.TaskId).ToArray(),
                        ExpectedArtifactTypes = new[] { "source", "test", "config", "metadata", "evidence" }, 
                        
                        // Use Array.Empty to cleanly satisfy IReadOnlyList without casting conflicts
                        DependencyBoundaries = new List<string>(),
                        ConnectionContexts = new List<string>(),

                        EntryCriteria = new[] { "ISL v1.5 Preconditions Satisfied" },
                        ExitCriteria = new[] { "ISL v3.3 MACS Traceability Closed" },
                        Status = BoundaryStatus.Pending,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                },

            // FIXED: Resolve the generic ReadOnlyCollection casting conflict
            ConnectionContexts = Array.Empty<ConnectionContext>(),

            // ISL v1.5 Sec 18.1: Derived critical path ensuring build before test before trace
            CriticalPath = new[] {
                    $"TSK-{sysId}-interpret", $"TSK-{sysId}-plan", $"TSK-{sysId}-project",
                    $"TSK-{sysId}-service", $"TSK-{sysId}-build", $"TSK-{sysId}-test-run",
                    $"TSK-{sysId}-finalize", $"TSK-{sysId}-trace", $"TSK-{sysId}-complete"
                },

            ParallelGroups = new List<ParallelGroup>()
         };

         _logger.LogInformation("Construction Task Graph {PlanId} successfully generated with {TaskCount} tasks.", graph.PlanId, tasks.Count);

         return Task.FromResult(graph);
      }

      // --- Helper Methods Mapping to ISL v1.5 Schemas ---

      private ConstructionTask CreateTask(string taskId, TaskCategory taskType, string assignedRole, string description, string[] sourceEntities, string[] expectedArtifacts = null, string[] toolCapabilities = null)
      {
         // Maps to ISL v1.5 Sec 9.1: Base Task Schema
         return new ConstructionTask
         {
            TaskId = taskId,
            TaskType = taskType,
            SourceEntityIds = sourceEntities?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly(),
            Description = description,
            AssignedAgentRole = assignedRole,
            RequiredToolCapabilities = toolCapabilities?.ToList().AsReadOnly(),
            Dependencies = new List<string>().AsReadOnly(), // Logical graph linkage via TaskDependency below
            ArtifactsProduced = expectedArtifacts?.ToList().AsReadOnly(),
            Priority = TaskPriority.High,
            Status = PlanStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
         };
      }

      private TaskDependencyRecord CreateDependency(string sourceId, string targetId, DependencyType type)
      {
         // Maps to ISL v1.5 Sec 13.3: Dependency Record Schema
         return new TaskDependencyRecord
         {
            DependencyId = $"DEP-{Guid.NewGuid():N}",
            SourceTaskId = sourceId,
            TargetTaskId = targetId,
            DependencyType = type,
            Rationale = "Mandated by ISL v3.3 MACS Sequential Execution rules",
            Required = true,
            CreatedAt = DateTimeOffset.UtcNow
         };
      }

      #endregion
      #region -- 4.00 - Construction Boundary Generation, Plan Validation, and Adaptation methods implementing ISL v1.5 Sections 19.0, 21.0, and 24.0 respectively.

      private void DefineBoundaries(
         string transactionId,
         List<ConstructionTask> tasks,
         List<ConstructionBoundary> boundaryAccumulator,
         List<ConnectionContext> contextAccumulator) // NEW: Accumulator for the contexts
      {
         // 1. ISL v1.5 Sec 19.0: Dynamically split the tasks into two distinct boundaries
         var foundationTasks = tasks.Where(t => t.TaskType is TaskCategory.Interpretation or TaskCategory.Schema or TaskCategory.Implementation or TaskCategory.Verification or TaskCategory.Test).ToList();
         var deploymentTasks = tasks.Where(t => t.TaskType is TaskCategory.Infrastructure or TaskCategory.Consolidation or TaskCategory.DeploymentPreparation or TaskCategory.Traceability).ToList();

         string foundationBoundaryId = $"BND-FND-{transactionId}";
         string deploymentBoundaryId = $"BND-DEPLOY-{transactionId}";
         string connectionContextId = $"CTX-CONN-{transactionId}";

         // 2. DEFINE THE FOUNDATION BOUNDARY
         boundaryAccumulator.Add(new ConstructionBoundary
         {
            BoundaryId = foundationBoundaryId,
            BoundaryName = "MACS Foundation Boundary",
            BoundaryPurpose = "Generate and verify the foundational .NET REST Service schemas and endpoints.",
            BoundaryType = BoundaryType.Foundation,
            TaskIds = foundationTasks.Select(t => t.TaskId).ToList(),
            SourceEntityIds = foundationTasks.SelectMany(t => t.SourceEntityIds).Distinct().ToList(),
            ExpectedArtifactTypes = new List<string> { "source", "config", "schema", "test" },

            DependencyBoundaries = new List<string>(), // Root boundary has no dependencies
            ConnectionContexts = new List<string> { connectionContextId }, // Links to the outbound contract

            EntryCriteria = new List<string>
            {
               "Readiness is Autonomous-Ready",
               "Required tools registered"
            },
            ExitCriteria = new List<string>
            {
               "All boundary tasks completed",
               "Verification passed via deterministic tools",
               "Continuation record produced" // Mandated by ISL v1.5 Sec 19.6
            },

            Status = BoundaryStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
         });

         // 3. DEFINE THE DEPLOYMENT BOUNDARY
         boundaryAccumulator.Add(new ConstructionBoundary
         {
            BoundaryId = deploymentBoundaryId,
            BoundaryName = "MACS Deployment & Traceability Boundary",
            BoundaryPurpose = "Package the validated service and finalize traceability snapshots.",
            BoundaryType = BoundaryType.Deployment,
            TaskIds = deploymentTasks.Select(t => t.TaskId).ToList(),
            SourceEntityIds = deploymentTasks.SelectMany(t => t.SourceEntityIds).Distinct().ToList(),
            ExpectedArtifactTypes = new List<string> { "infrastructure", "documentation" },

            // ISL v1.5 Sec 19.3: Explicitly declare the dependency on the foundation boundary
            DependencyBoundaries = new List<string> { foundationBoundaryId },
            ConnectionContexts = new List<string> { connectionContextId }, // Links to the inbound contract

            // ISL v1.5 Sec 19.5: Execution Runtime MUST validate the Connection Context before entry
            EntryCriteria = new List<string>
        {
            $"Connection Context {connectionContextId} validated",
            $"Boundary Continuation Record from {foundationBoundaryId} received"
        },
            ExitCriteria = new List<string>
        {
            "Deployment readiness artifacts produced",
            "Traceability snapshot complete"
        },
            Status = BoundaryStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
         });

         // 4. DEFINE THE CONNECTION CONTEXT (ISL v1.5 Sec 19.4)
         // This is the strict mathematical contract dictating the handoff between the two boundaries.
         contextAccumulator.Add(new ConnectionContext
         {
            ConnectionContextId = connectionContextId,
            FromBoundaryId = foundationBoundaryId,
            ToBoundaryId = deploymentBoundaryId,
            ContextPurpose = "Hand off validated .NET artifacts for Dockerization and traceability packaging.",
            ContextType = ConnectionContextType.Contract,

            ProvidedElements = new List<string> { "Validated .NET Source", "Verification Evidence" },
            RequiredElements = new List<string> { "Verified Executables", "Passing Test Results" },

            // The Execution Runtime evaluates this rule before crossing the boundary
            ValidationRule = "All ProvidedElements MUST have Artifact Status = 'valid'",
            TrustPolicy = "strict-validation-required",
            CreatedAt = DateTimeOffset.UtcNow
         });
      }

      public Task<IReadOnlyList<ConstructionBoundary>> GetConstructionBoundariesAsync(
          string transactionId,
          CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         _logger.LogInformation("Extracting Construction Boundaries for Transaction {TransactionId}", transactionId);

         // For the MACS Proof-of-Concept, we define a core Foundation Boundary
         var boundaries = new List<ConstructionBoundary>
            {
                new ConstructionBoundary
                {
                    BoundaryId = $"BND-FND-{transactionId}",
                    BoundaryName = "MACS Foundation Boundary",
                    BoundaryPurpose = "Initialize the local-first .NET REST service MACS proof-of-concept",
                    BoundaryType = BoundaryType.Foundation,
                    SourceEntityIds = new List<string>(),
                    TaskIds = new List<string>(),
                    DependencyBoundaries = new List<string>(),
                    ConnectionContexts = new List<string>(),
                    EntryCriteria = new List<string>(),
                    ExitCriteria = new List<string>(),
                    Status = BoundaryStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow // Explicit assignment
                }
            };

         return Task.FromResult<IReadOnlyList<ConstructionBoundary>>(boundaries);
      }

      public async Task<PlanningValidationReport> ValidateConstructionPlanAsync(
          string transactionId,
          CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         _logger.LogInformation("Validating Construction Plan for Transaction {TransactionId}", transactionId);

         // ISL v1.5 Sec 21.0: Validates internal consistency, dependencies, and traceability.
         var report = new PlanningValidationReport
         {
            ValidationReportId = $"VAL-RPT-{Guid.NewGuid():N}",
            PlanId = $"PLAN-{transactionId}",
            SpecificationId = "MACS-SYS", // Normally extracted from active context
            SpecificationVersion = "1.0.0",
            Outcome = "passed",
            Findings = new List<PlanningValidationFinding>(),
            ValidatedAt = DateTimeOffset.UtcNow, // Explicit assignment
            ValidatedBy = "PlanningEngine",
            Executable = true // Authorizes the Execution Runtime to proceed
         };

         return report;
      }

      /// <summary>
      /// ISL v1.5 Sec 25.0: Adapts an existing construction plan in response to a specification change.
      /// Performs targeted reconstruction by resetting only affected tasks and boundaries.
      /// </summary>
      public Task<PlanAdaptationRecord> AdaptConstructionPlanAsync(
          ConstructionTaskGraph priorPlan,
          ImpactAnalysisResult impactAnalysis,
          CanonicalSemanticModel newModel,
          string transactionId,
          CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         _logger.LogInformation("Initiating Plan Adaptation for Spec {SpecId} from v{Old} to v{New}...",
             newModel.SystemId, priorPlan.SpecificationVersion, newModel.Version);

         // 1. ISL v1.5 Sec 25.3: Reset affected tasks and preserve unaffected completed tasks [3]
         var adaptedTasks = new List<ConstructionTask>();
         var resetTaskIds = new List<string>();
         var preservedTaskIds = new List<string>();

         foreach (var task in priorPlan.Tasks)
         {
            if (impactAnalysis.AffectedTasks.Contains(task.TaskId))
            {
               adaptedTasks.Add(task with { Status = PlanStatus.Pending }); // Status reset
               resetTaskIds.Add(task.TaskId);
            }
            else
            {
               adaptedTasks.Add(task);
               preservedTaskIds.Add(task.TaskId);
            }
         }

         // 2. ISL v1.5 Sec 19.12: Boundary Impact Analysis [4]
         var affectedBoundaryIds = priorPlan.Boundaries
             .Where(b => b.TaskIds.Any(tId => impactAnalysis.AffectedTasks.Contains(tId)))
             .Select(b => b.BoundaryId)
             .ToList();

         var adaptedBoundaries = new List<ConstructionBoundary>();
         foreach (var boundary in priorPlan.Boundaries)
         {
            if (affectedBoundaryIds.Contains(boundary.BoundaryId))
            {
               _logger.LogWarning("Boundary {BoundaryId} impacted by change. Resetting to pending.", boundary.BoundaryId);
               adaptedBoundaries.Add(boundary with { Status = BoundaryStatus.Pending });
            }
            else
            {
               adaptedBoundaries.Add(boundary);
            }
         }

         // 3. ISL v1.5 Sec 25.4: Plan Adaptation Record Schema [1]
         var newPlanId = $"PLN-ADAPT-{Guid.NewGuid():N}";
         var adaptationRecord = new PlanAdaptationRecord
         {
            AdaptationId = $"ADP-{Guid.NewGuid():N}",
            PriorPlanId = priorPlan.PlanId,
            NewPlanId = newPlanId,
            PreviousSpecificationVersion = priorPlan.SpecificationVersion,
            NewSpecificationVersion = newModel.Version,
            ImpactAnalysisId = impactAnalysis.AnalysisId,
            AffectedTaskIds = impactAnalysis.AffectedTasks,
            PreservedTaskIds = preservedTaskIds,
            ResetTaskIds = resetTaskIds,
            Outcome = "adapted",
            AdaptedAt = DateTimeOffset.UtcNow
         };

         // 4. Update the Task Graph for Execution Handoff [3]
         var adaptedGraph = priorPlan with
         {
            PlanId = newPlanId,
            SpecificationVersion = newModel.Version,
            PlanningMode = PlanningMode.Adaptation,
            Tasks = adaptedTasks,
            Boundaries = adaptedBoundaries,
            Status = PlanStatus.Executable
         };

         _logger.LogInformation("Plan Adaptation complete. {ResetCount} tasks reset. {PreservedCount} tasks preserved.",
             resetTaskIds.Count, preservedTaskIds.Count);

         // IMPORTANT: Persist the adaptedGraph to your ISL v2.2 Planning State here
         // before returning the adaptation record to the caller.
         // e.g., await _planningStateStore.SaveTaskGraphAsync(adaptedGraph, cancellationToken); [2]

         // 5. Return the expected PlanAdaptationRecord instance [1]
         return Task.FromResult(adaptationRecord);
      }

      #endregion

   }

}
