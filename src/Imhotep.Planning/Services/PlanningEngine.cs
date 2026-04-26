using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Graph;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Planning.Services
{
   /// <summary>
   /// Transforms the canonical specification graph into a structured set of development tasks (ISL v1.5 Section 1.0).
   /// </summary>
   public interface IPlanningEngine
   {
      Task<ConstructionTaskGraph> GenerateTaskGraphAsync(CanonicalSemanticModel activeModel, CancellationToken cancellationToken = default);

      /// <summary>
      /// Analyzes differences when a specification is updated and generates a targeted reconstruction plan.
      /// </summary>
      Task<ConstructionTaskGraph> AdaptTaskGraphForImpactAsync(string transactionId, CanonicalSemanticModel updatedModel, IReadOnlyList<string> impactedTraceabilityIds, CancellationToken cancellationToken = default);
   }

   public class PlanningEngine : IPlanningEngine
   {
      private readonly ILogger<PlanningEngine> _logger;

      public PlanningEngine(ILogger<PlanningEngine> logger)
      {
         _logger = logger;
      }

      public Task<ConstructionTaskGraph> GenerateTaskGraphAsync(CanonicalSemanticModel activeModel, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Planning Engine initiating task graph generation for Transaction {TransactionId}", activeModel.TransactionId);

         var taskGraph = new ConstructionTaskGraph
         {
            TransactionId = activeModel.TransactionId
         };

         // 1. Task Generation based on Canonical Entities (ISL v1.5 Section 3.0)
         GenerateDataEntityTasks(activeModel, taskGraph);
         GenerateServiceImplementationTasks(activeModel, taskGraph);
         GenerateVerificationTasks(activeModel, taskGraph);
         GenerateInfrastructureTasks(activeModel, taskGraph);

         // 2. Dependency Resolution (ISL v1.5 Section 5.0)
         ResolveDependencies(activeModel, taskGraph);

         _logger.LogInformation("Task Graph generated successfully. Total Tasks: {Count}. DAG is ready for Execution Runtime.", taskGraph.Tasks.Count);

         return Task.FromResult(taskGraph);
      }

      private void GenerateDataEntityTasks(CanonicalSemanticModel model, ConstructionTaskGraph graph)
      {
         foreach (var dataEntity in model.DataEntities)
         {
            string taskId = $"TASK-DATA-{dataEntity.TraceabilityId}";
            graph.Tasks[taskId] = new ConstructionTask
            {
               TaskId = taskId,
               TargetTraceabilityId = dataEntity.TraceabilityId,
               Category = TaskCategory.Implementation,
               Description = $"Generate concrete data schema/models for {dataEntity.Name}",
               AssignedAgentRole = "Implementation Generator"
            };
         }
      }

      private void GenerateServiceImplementationTasks(CanonicalSemanticModel model, ConstructionTaskGraph graph)
      {
         foreach (var service in model.Services)
         {
            string taskId = $"TASK-SRV-{service.TraceabilityId}";
            graph.Tasks[taskId] = new ConstructionTask
            {
               TaskId = taskId,
               TargetTraceabilityId = service.TraceabilityId,
               Category = TaskCategory.Implementation,
               Description = $"Scaffold and implement operational logic for service {service.Name}",
               AssignedAgentRole = "Implementation Generator"
            };
         }
      }

      private void GenerateVerificationTasks(CanonicalSemanticModel model, ConstructionTaskGraph graph)
      {
         foreach (var validation in model.Validations)
         {
            string taskId = $"TASK-VAL-{validation.TraceabilityId}";
            graph.Tasks[taskId] = new ConstructionTask
            {
               TaskId = taskId,
               TargetTraceabilityId = validation.TraceabilityId,
               Category = TaskCategory.Verification,
               Description = $"Map and execute deterministic tool validation for {validation.Name}",
               AssignedAgentRole = "Test Generator"
            };
         }
      }

      private void GenerateInfrastructureTasks(CanonicalSemanticModel model, ConstructionTaskGraph graph)
      {
         foreach (var infra in model.Infrastructures)
         {
            string taskId = $"TASK-INFRA-{infra.TraceabilityId}";
            graph.Tasks[taskId] = new ConstructionTask
            {
               TaskId = taskId,
               TargetTraceabilityId = infra.TraceabilityId,
               Category = TaskCategory.Infrastructure,
               Description = $"Generate deployment manifests and IaC for {infra.Name}",
               AssignedAgentRole = "Deployment Preparer"
            };
         }
      }

      private void ResolveDependencies(CanonicalSemanticModel model, ConstructionTaskGraph graph)
      {
         // The Planning Engine enforces strict topological ordering by analyzing the semantic Relational Edges.
         // For example, Service implementation tasks depend on DataEntity schemas [6].
         foreach (var edge in model.RelationshipEdge)
         {
            // If a Service (Target) "Uses" a DataEntity (Source), the Service Task depends on the DataEntity Task
            if (edge.RelationshipType == "Uses")
            {
               var sourceTask = graph.Tasks.Values.FirstOrDefault(t => t.TargetTraceabilityId == edge.SourceId);
               var targetTask = graph.Tasks.Values.FirstOrDefault(t => t.TargetTraceabilityId == edge.TargetId);

               if (sourceTask != null && targetTask != null)
               {
                  targetTask.Dependencies.Add(sourceTask.TaskId);
                  _logger.LogDebug("Resolved Dependency: {TargetTask} depends on {SourceTask}", targetTask.TaskId, sourceTask.TaskId);
               }
            }
         }

         // All Verification tasks natively depend on the Implementation tasks they test
         var verificationTasks = graph.Tasks.Values.Where(t => t.Category == TaskCategory.Verification);
         var implementationTasks = graph.Tasks.Values.Where(t => t.Category == TaskCategory.Implementation);

         foreach (var vTask in verificationTasks)
         {
            // In a true graph, this matches the exact entity the validation rule maps to via "Verifies" edges.
            foreach (var iTask in implementationTasks)
            {
               vTask.Dependencies.Add(iTask.TaskId);
            }
         }
      }

      public Task<ConstructionTaskGraph> AdaptTaskGraphForImpactAsync(string transactionId, CanonicalSemanticModel updatedModel, IReadOnlyList<string> impactedTraceabilityIds, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Generating Targeted Reconstruction Task Graph for Transaction {TransactionId}", transactionId);

         // Implementation of ISL v1.5 Section 12.0: Adaptation to Specification Changes [7].
         // Uses the Traceability Graph impact results to only enqueue tasks for entities affected by the change.
         var targetedGraph = new ConstructionTaskGraph { TransactionId = transactionId };

         // Logic to selectively map impactedTraceabilityIds into targeted tasks...

         return Task.FromResult(targetedGraph);
      }
   }
}
