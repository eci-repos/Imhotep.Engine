using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Traceability.Models;

namespace Imhotep.Traceability.Services
{
   /// <summary>
   /// Maintains the bidirectional graph linking specifications to tasks and artifacts, 
   /// enabling automated impact analysis and continuous evolution (ISL v1.4).
   /// </summary>
   public interface ITraceabilityService
   {
      Task RegisterNodeAsync(TraceabilityNode node);
      Task CreateEdgeAsync(TraceabilityEdge edge);

      // Bidirectional Navigation
      Task<IReadOnlyList<TraceabilityNode>> GetDownstreamArtifactsAsync(string specificationNodeId);
      Task<IReadOnlyList<TraceabilityNode>> GetUpstreamSpecificationsAsync(string artifactNodeId);

      // Enterprise Day 2 Operations
      Task<IReadOnlyList<TraceabilityNode>> PerformImpactAnalysisAsync(string modifiedSpecificationId);
   }

   public class TraceabilityService : ITraceabilityService
   {
      private readonly ILogger<TraceabilityService> _logger;

      // In a production environment, this would be backed by a specialized Graph Database 
      // (e.g., Neo4j, Cosmos DB Gremlin API) to handle deep traversal.
      private readonly Dictionary<string, TraceabilityNode> _nodes = new();
      private readonly List<TraceabilityEdge> _edges = new();

      public TraceabilityService(ILogger<TraceabilityService> logger)
      {
         _logger = logger;
      }

      public Task RegisterNodeAsync(TraceabilityNode node)
      {
         if (!_nodes.ContainsKey(node.NodeId))
         {
            _nodes[node.NodeId] = node;
            _logger.LogDebug("Registered Traceability Node: {NodeId} [{Type}]", node.NodeId, node.Type);
         }
         return Task.CompletedTask;
      }

      public Task CreateEdgeAsync(TraceabilityEdge edge)
      {
         _edges.Add(edge);
         _logger.LogDebug("Created Edge: {Source} -[{Relationship}]-> {Target}",
             edge.SourceNodeId, edge.Relationship, edge.TargetNodeId);
         return Task.CompletedTask;
      }

      public Task<IReadOnlyList<TraceabilityNode>> GetDownstreamArtifactsAsync(string specificationNodeId)
      {
         // Traces from the Blueprint DOWN to the physical implementation
         // Answers: "What files were generated from REQ-001?"
         var downstreamIds = _edges
             .Where(e => e.TargetNodeId == specificationNodeId && e.Relationship == RelationshipType.Fulfills)
             .Select(e => e.SourceNodeId)
             .ToList();

         var artifacts = downstreamIds.Select(id => _nodes[id]).ToList();
         return Task.FromResult<IReadOnlyList<TraceabilityNode>>(artifacts);
      }

      public Task<IReadOnlyList<TraceabilityNode>> GetUpstreamSpecificationsAsync(string artifactNodeId)
      {
         // Traces from the Implementation UP to the Blueprint
         // Answers: "Why does this C# file exist?"
         var upstreamIds = _edges
             .Where(e => e.SourceNodeId == artifactNodeId && e.Relationship == RelationshipType.Fulfills)
             .Select(e => e.TargetNodeId)
             .ToList();

         var specs = upstreamIds.Select(id => _nodes[id]).ToList();
         return Task.FromResult<IReadOnlyList<TraceabilityNode>>(specs);
      }

      public Task<IReadOnlyList<TraceabilityNode>> PerformImpactAnalysisAsync(string modifiedSpecificationId)
      {
         _logger.LogInformation("Performing Automated Impact Analysis for Specification Update: {SpecId}", modifiedSpecificationId);

         // In a true graph database, this would be a recursive query (e.g., searching depth 1 to N).
         // This simplified recursive search identifies all downstream components affected by a spec change.
         var impactedNodes = new HashSet<TraceabilityNode>();
         var queue = new Queue<string>();
         queue.Enqueue(modifiedSpecificationId);

         while (queue.Any())
         {
            var currentId = queue.Dequeue();

            // Find all edges where the current node is the target (i.e., things depending on it)
            var affectedIds = _edges
                .Where(e => e.TargetNodeId == currentId)
                .Select(e => e.SourceNodeId);

            foreach (var affectedId in affectedIds)
            {
               if (_nodes.TryGetValue(affectedId, out var node) && impactedNodes.Add(node))
               {
                  queue.Enqueue(affectedId); // Recurse deeper
               }
            }
         }

         _logger.LogInformation("Impact Analysis identified {Count} components requiring targeted reconstruction.", impactedNodes.Count);
         return Task.FromResult<IReadOnlyList<TraceabilityNode>>(impactedNodes.ToList());
      }
   }
}
