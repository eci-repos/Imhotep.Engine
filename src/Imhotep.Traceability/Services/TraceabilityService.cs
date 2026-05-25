using Imhotep.Traceability.Models;
using Imhotep.Planning.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Imhotep.Traceability.Services;

/// <summary>
/// ISL v1.4: Traceability Engine.
/// Maintains the persistent graph linking specification entities, tasks, artifacts, 
/// validations, repairs, governance events, and boundaries.
/// </summary>
public interface ITraceabilityService
{
   /// <summary>
   /// Records a single node in the Traceability Graph.
   /// </summary>
   Task RecordNodeAsync(TraceabilityNode node, CancellationToken cancellationToken = default);

   /// <summary>
   /// Records a directional relationship edge between two nodes.
   /// </summary>
   Task RecordEdgeAsync(TraceabilityEdge edge, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.5 Sec 19.8: Helper to explicitly map a completed boundary and its continuation record.
   /// (This directly satisfies the commented-out code in our ExecutionService loop).
   /// </summary>
   Task RecordBoundaryCompletionAsync(
       string boundaryId,
       string continuationRecordId,
       string specificationId,
       string specificationVersion,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.5 Sec 19.8 (Boundary Traceability): Seeds the traceability graph with the initial 
   /// Construction Boundary nodes, connection contexts, and structural edges prior to execution.
   /// </summary>
   Task InitializeBoundaryTraceabilityAsync(
       ConstructionTaskGraph plan,
       IReadOnlyList<ConnectionContext> connectionContexts,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.4 Sec 18.0: Creates an immutable snapshot of the current traceability graph state.
   /// Required before deployment preparation or crossing major governance boundaries.
   /// </summary>
   Task<TraceabilitySnapshot> CreateSnapshotAsync(
       string specificationId,
       string specificationVersion,
       string purpose,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.4 Sec 17.0: Evaluates what artifacts, tasks, or policies are affected 
   /// when a specification blueprint changes (Day 2 Operations).
   /// </summary>
   Task<ImpactAnalysisResult> PerformImpactAnalysisAsync(
       string changeTriggerId,
       string specificationId,
       string newSpecificationVersion,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.4 Sec 21.1: Mandatory Audit Query (Reverse Traceability).
   /// Given an artifact, returns the originating entities, tasks, and decisions that caused its existence.
   /// </summary>
   Task<IReadOnlyList<TraceabilityNode>> GetArtifactOriginAsync(
       string artifactId,
       CancellationToken cancellationToken = default);
}

public class TraceabilityService : ITraceabilityService
{
   private readonly ILogger<TraceabilityService> _logger;

   // ISL v2.2 Sec 6.1: The Graph Store maintaining canonical, traceability, and dependency relationships.
   // Simulated here for the MACS POC using thread-safe dictionaries.
   private readonly ConcurrentDictionary<string, TraceabilityNode> _nodeStore = new();
   private readonly ConcurrentDictionary<string, TraceabilityEdge> _edgeStore = new();
   private readonly ConcurrentDictionary<string, TraceabilitySnapshot> _snapshotStore = new();

   public TraceabilityService(ILogger<TraceabilityService> logger)
   {
      _logger = logger;
   }

   /// <summary>
   /// Records a single node in the Traceability Graph.
   /// </summary>
   public Task RecordNodeAsync(TraceabilityNode node, CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // ISL v1.4 Sec 8.3: A node MUST NOT exist without a valid node-id and MUST identify the specification version.
      if (string.IsNullOrWhiteSpace(node.NodeId))
         throw new ArgumentException("Traceability Node MUST have a valid NodeId.");

      if (string.IsNullOrWhiteSpace(node.SpecificationVersion))
         throw new ArgumentException("Traceability Node MUST identify the specification version.");

      // ISL v1.4 Sec 5.2: Traceability relationships MUST be recorded when the related action occurs.
      if (_nodeStore.TryAdd(node.NodeId, node))
      {
         _logger.LogInformation("Traceability Node Recorded: {NodeId} [{NodeType}] for Spec {SpecId} v{Version}",
             node.NodeId, node.NodeType, node.SpecificationId, node.SpecificationVersion);
      }
      else
      {
         // ISL v1.4 Sec 9.3: Historical traceability must be preserved. Corrections are supersessions, not overwrites.
         _logger.LogWarning("Traceability Node {NodeId} already exists. In-place updates to historical nodes are restricted.", node.NodeId);
      }

      return Task.CompletedTask;
   }

   /// <summary>
   /// Records a directional relationship edge between two nodes.
   /// </summary>
   public Task RecordEdgeAsync(TraceabilityEdge edge, CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // ISL v1.4 Sec 9.2 & 9.3: Required identifier and relationship validation
      if (string.IsNullOrWhiteSpace(edge.EdgeId))
         throw new ArgumentException("Traceability Edge MUST have a valid EdgeId.");

      if (string.IsNullOrWhiteSpace(edge.SourceNodeId) || string.IsNullOrWhiteSpace(edge.TargetNodeId))
         throw new ArgumentException("Traceability Edge MUST define both SourceNodeId and TargetNodeId.");

      // ISL v1.4 Sec 9.3: Inferred relationships must justify their existence
      if (edge.Confidence == "inferred" && string.IsNullOrWhiteSpace(edge.Rationale))
         throw new ArgumentException("Inferred traceability edges MUST include a Rationale [ISL v1.4 Sec 9.3].");

      // Ideally, in a full database-backed implementation, we would also verify:
      // if (!_nodeStore.ContainsKey(edge.SourceNodeId) || !_nodeStore.ContainsKey(edge.TargetNodeId))
      //     throw new InvalidOperationException("Every edge source and target MUST resolve to existing traceability nodes.");

      // ISL v1.4 Sec 9.3: Edges MUST NOT be deleted or silently overwritten.
      if (_edgeStore.TryAdd(edge.EdgeId, edge))
      {
         _logger.LogInformation("Traceability Edge Recorded: {EdgeId} [{EdgeType}] linking {Source} -> {Target}",
             edge.EdgeId, edge.EdgeType, edge.SourceNodeId, edge.TargetNodeId);
      }
      else
      {
         _logger.LogWarning("Traceability Edge {EdgeId} already exists. In-place updates are restricted.", edge.EdgeId);
      }

      return Task.CompletedTask;
   }

   /// <summary>
   /// ISL v1.5 Sec 19.8: Helper to explicitly map a completed boundary and its continuation record.
   /// </summary>
   public async Task RecordBoundaryCompletionAsync(
       string boundaryId,
       string continuationRecordId,
       string specificationId,
       string specificationVersion,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(boundaryId) || string.IsNullOrWhiteSpace(continuationRecordId))
         throw new ArgumentException("Both boundaryId and continuationRecordId MUST be provided to establish a traceability link.");

      _logger.LogInformation("Recording boundary completion traceability for Boundary {BoundaryId} -> Continuation {ContinuationId}",
          boundaryId, continuationRecordId);

      // 1. ISL v1.5 Sec 19.8: Register the Boundary Continuation Record as a first-class node
      var continuationNode = new TraceabilityNode
      {
         NodeId = continuationRecordId,
         NodeType = "BoundaryContinuationRecord",
         SpecificationId = specificationId,
         SpecificationVersion = specificationVersion,
         CreatedBy = "Execution Runtime", // The platform component executing the boundary
         CreatedAt = DateTimeOffset.UtcNow,
         Status = "active"
      };

      await RecordNodeAsync(continuationNode, cancellationToken);

      // 2. ISL v1.5 Sec 19.8: Create the explicit 'produces-continuation' edge
      var continuationEdge = new TraceabilityEdge
      {
         EdgeId = $"EDG-BND-CONT-{Guid.NewGuid():N}",
         EdgeType = "produces-continuation", // Explicitly mandated edge type
         SourceNodeId = boundaryId,          // Source = The Boundary
         TargetNodeId = continuationRecordId, // Target = The Continuation Record
         SpecificationId = specificationId,
         SpecificationVersion = specificationVersion,
         CreatedBy = "Execution Runtime",
         CreatedAt = DateTimeOffset.UtcNow,
         Confidence = "explicit" // This is a deterministic structural link, not a guess
      };

      await RecordEdgeAsync(continuationEdge, cancellationToken);
   }

   /// <summary>
   /// ISL v1.4 Sec 18.0: Creates an immutable snapshot of the current traceability graph state.
   /// Required before deployment preparation or crossing major governance boundaries.
   /// </summary>
   public Task<TraceabilitySnapshot> CreateSnapshotAsync(
       string specificationId,
       string specificationVersion,
       string purpose,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(specificationId) || string.IsNullOrWhiteSpace(specificationVersion))
         throw new ArgumentException("SpecificationId and SpecificationVersion MUST be provided to create a snapshot.");

      // ISL v1.4 Sec 18.2: Purpose must be defined (e.g., readiness, execution-start, consolidation, deployment)
      if (string.IsNullOrWhiteSpace(purpose))
         throw new ArgumentException("SnapshotPurpose MUST be provided.");

      _logger.LogInformation("Creating Traceability Snapshot for Spec {SpecId} v{Version}. Purpose: {Purpose}",
          specificationId, specificationVersion, purpose);

      // In a production Graph DB, we would query the exact subgraph.
      // For the MACS POC in-memory store, we dynamically filter the dictionaries.
      int nodeCount = _nodeStore.Values.Count(n => n.SpecificationId == specificationId && n.SpecificationVersion == specificationVersion);
      int edgeCount = _edgeStore.Values.Count(e => e.SpecificationId == specificationId && e.SpecificationVersion == specificationVersion);

      // 1. ISL v1.4 Sec 18.2: Instantiate the Snapshot Schema
      var snapshot = new TraceabilitySnapshot
      {
         SnapshotId = $"TRS-{Guid.NewGuid():N}",
         SpecificationId = specificationId,
         SpecificationVersion = specificationVersion,
         CanonicalModelVersion = specificationVersion, // Assuming 1:1 alignment for the POC
         CreatedBy = "Traceability Engine",
         CreatedAt = DateTimeOffset.UtcNow,
         NodeCount = nodeCount,
         EdgeCount = edgeCount,
         SnapshotPurpose = purpose,
         StorageLocation = "in-memory-poc-store",
         IntegrityHash = "POC-NO-HASH" // In production, this would be a SHA-256 hash of the graph nodes
      };

      // 2. ISL v1.4 Sec 18.3: Snapshots MUST be immutable once created. A later snapshot MUST NOT overwrite an earlier one.
      if (!_snapshotStore.TryAdd(snapshot.SnapshotId, snapshot))
      {
         throw new InvalidOperationException($"Failed to securely persist Traceability Snapshot {snapshot.SnapshotId}.");
      }

      _logger.LogInformation("Traceability Snapshot {SnapshotId} successfully created containing {NodeCount} nodes and {EdgeCount} edges.",
          snapshot.SnapshotId, snapshot.NodeCount, snapshot.EdgeCount);

      return Task.FromResult(snapshot);
   }

   /// <summary>
   /// ISL v1.4 Sec 17.0: Evaluates what artifacts, tasks, or policies are affected 
   /// when a specification blueprint changes (Day 2 Operations).
   /// </summary>
   public Task<ImpactAnalysisResult> PerformImpactAnalysisAsync(
       string changeTriggerId,
       string specificationId,
       string newSpecificationVersion,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(changeTriggerId) || string.IsNullOrWhiteSpace(specificationId))
         throw new ArgumentException("ChangeTriggerId and SpecificationId MUST be provided for Impact Analysis.");

      _logger.LogInformation("Performing Impact Analysis for Spec {SpecId} (Targeting Version {NewVersion}) triggered by {TriggerId}",
          specificationId, newSpecificationVersion, changeTriggerId);

      // In a production environment, this would execute a recursive Graph DB traversal (e.g., Neo4j).
      // For the MACS POC, we simulate tracing downstream edges from the changed entity.
      var affectedTasks = new List<string>();
      var affectedArtifacts = new List<string>();
      var unaffectedArtifacts = new List<string>();

      // 1. ISL v1.4 Sec 17.3: Impact analysis MUST traverse the traceability graph 
      // from changed entities through dependent tasks and artifacts.
      var directEdges = _edgeStore.Values.Where(e => e.SourceNodeId == changeTriggerId).ToList();

      foreach (var edge in directEdges)
      {
         if (_nodeStore.TryGetValue(edge.TargetNodeId, out var targetNode))
         {
            if (targetNode.NodeType == "ConstructionTask")
               affectedTasks.Add(targetNode.NodeId);
            else if (targetNode.NodeType == "Artifact")
               affectedArtifacts.Add(targetNode.NodeId);
         }
      }

      // Simulate classifying the rest as unaffected for the POC
      var allArtifactNodes = _nodeStore.Values.Where(n => n.NodeType == "Artifact" && n.SpecificationId == specificationId);
      foreach (var art in allArtifactNodes)
      {
         if (!affectedArtifacts.Contains(art.NodeId))
            unaffectedArtifacts.Add(art.NodeId);
      }

      // 2. Instantiate the compliant Impact Analysis Result
      var result = new ImpactAnalysisResult
      {
         AnalysisId = $"IAN-{Guid.NewGuid():N}",
         TriggeredBy = changeTriggerId,
         SpecificationId = specificationId,
         NewSpecificationVersion = newSpecificationVersion,
         ChangedEntities = new List<string> { changeTriggerId },
         AffectedTasks = affectedTasks,
         AffectedArtifacts = affectedArtifacts,
         UnaffectedArtifacts = unaffectedArtifacts,
         AnalysisMethod = "poc-in-memory-traversal",
         Confidence = "partial", // Labeled 'partial' because the POC simulates 1-level depth traversal
         AnalysisTimestamp = DateTime.UtcNow
      };

      // 3. ISL v1.4 Sec 17.3: Impact analysis results MUST be recorded as traceability nodes
      var analysisNode = new TraceabilityNode
      {
         NodeId = result.AnalysisId,
         NodeType = "ImpactAnalysis",
         SpecificationId = specificationId,
         SpecificationVersion = newSpecificationVersion,
         CreatedBy = "Traceability Engine",
         CreatedAt = DateTimeOffset.UtcNow,
         Status = "active"
      };

      _nodeStore.TryAdd(analysisNode.NodeId, analysisNode);

      _logger.LogInformation("Impact Analysis {AnalysisId} completed. Affected Tasks: {TaskCount}, Affected Artifacts: {ArtCount}",
          result.AnalysisId, affectedTasks.Count, affectedArtifacts.Count);

      return Task.FromResult(result);
   }

   /// <summary>
   /// ISL v1.4 Sec 21.1: Mandatory Audit Query (Reverse Traceability).
   /// Given an artifact, returns the originating entities, tasks, and decisions that caused its existence.
   /// </summary>
   public Task<IReadOnlyList<TraceabilityNode>> GetArtifactOriginAsync(
       string artifactId,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(artifactId))
         throw new ArgumentException("ArtifactId MUST be provided to execute a reverse traceability query.");

      _logger.LogInformation("Executing Reverse Traceability Audit Query for Artifact {ArtifactId}", artifactId);

      var originNodes = new List<TraceabilityNode>();
      var visitedEdges = new HashSet<string>();
      var queue = new Queue<string>();

      // Start the reverse traversal from the requested artifact
      queue.Enqueue(artifactId);

      // In a production platform, this reverse traversal would use a Graph DB query 
      // (e.g., in Neo4j: MATCH (a:Artifact {id: artifactId})<-[*]-(o) RETURN o).
      // For the MACS POC, we perform a breadth-first search backward through the in-memory edge store.
      while (queue.Any())
      {
         var currentNodeId = queue.Dequeue();

         // Find all edges where the current node is the TARGET (Reverse Traversal)
         var inboundEdges = _edgeStore.Values.Where(e => e.TargetNodeId == currentNodeId).ToList();

         foreach (var edge in inboundEdges)
         {
            if (visitedEdges.Add(edge.EdgeId))
            {
               if (_nodeStore.TryGetValue(edge.SourceNodeId, out var sourceNode))
               {
                  // Avoid adding duplicates to our result list if multiple paths converge
                  if (!originNodes.Any(n => n.NodeId == sourceNode.NodeId))
                  {
                     originNodes.Add(sourceNode);
                  }

                  // ISL v1.4 Sec 16.2: We traverse backward to find Tasks, Entities, Invocations, and Decisions
                  // We stop traversing once we hit a root SpecificationEntity to prevent infinite loops
                  if (sourceNode.NodeType != "SpecificationEntity")
                  {
                     queue.Enqueue(sourceNode.NodeId);
                  }
               }
            }
         }
      }

      _logger.LogInformation("Reverse Traceability completed. Found {Count} originating nodes for Artifact {ArtifactId}.",
          originNodes.Count, artifactId);

      return Task.FromResult<IReadOnlyList<TraceabilityNode>>(originNodes);
   }

   public async Task InitializeBoundaryTraceabilityAsync(
      ConstructionTaskGraph plan,
      IReadOnlyList<ConnectionContext> connectionContexts,
      CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Initializing boundary traceability for Plan {PlanId}", plan.PlanId);

      // 1. Process all Construction Boundaries [ISL v1.5 Sec 19.8]
      foreach (var boundary in plan.Boundaries)
      {
         // Register the Boundary Node itself
         var boundaryNode = new TraceabilityNode
         {
            NodeId = boundary.BoundaryId,
            NodeType = "ConstructionBoundary",
            SpecificationId = plan.SpecificationId,
            SpecificationVersion = plan.SpecificationVersion,
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "PlanningEngine"
         };
         await RecordNodeAsync(boundaryNode, cancellationToken);

         // Edge: 'contains-task'
         foreach (var taskId in boundary.TaskIds)
         {
            await RecordEdgeAsync(new TraceabilityEdge
            {
               EdgeId = $"EDG-CNT-{Guid.NewGuid():N}",
               EdgeType = "contains-task",
               SourceNodeId = boundary.BoundaryId,
               TargetNodeId = taskId,
               SpecificationId = plan.SpecificationId,
               SpecificationVersion = plan.SpecificationVersion,
               CreatedAt = DateTimeOffset.UtcNow,
               CreatedBy = "PlanningEngine"
            }, cancellationToken);
         }

         // Edge: 'depends-on-boundary'
         if (boundary.DependencyBoundaries != null)
         {
            foreach (var depId in boundary.DependencyBoundaries)
            {
               await RecordEdgeAsync(new TraceabilityEdge
               {
                  EdgeId = $"EDG-DEP-{Guid.NewGuid():N}",
                  EdgeType = "depends-on-boundary",
                  SourceNodeId = boundary.BoundaryId,
                  TargetNodeId = depId,
                  SpecificationId = plan.SpecificationId,
                  SpecificationVersion = plan.SpecificationVersion,
                  CreatedAt = DateTimeOffset.UtcNow,
                  CreatedBy = "PlanningEngine"
               }, cancellationToken);
            }
         }
      }

      // 2. Process all Connection Contexts (Cross-Boundary Contracts)
      if (connectionContexts != null)
      {
         foreach (var context in connectionContexts)
         {
            // Register the Connection Context node
            var contextNode = new TraceabilityNode
            {
               NodeId = context.ConnectionContextId,
               NodeType = "ConnectionContext",
               SpecificationId = plan.SpecificationId,
               SpecificationVersion = plan.SpecificationVersion,
               Status = "active",
               CreatedAt = DateTimeOffset.UtcNow,
               CreatedBy = "PlanningEngine"
            };
            await RecordNodeAsync(contextNode, cancellationToken);

            // Edge: 'provides-context' (From the Upstream Boundary)
            await RecordEdgeAsync(new TraceabilityEdge
            {
               EdgeId = $"EDG-PRV-{Guid.NewGuid():N}",
               EdgeType = "provides-context",
               SourceNodeId = context.FromBoundaryId,
               TargetNodeId = context.ConnectionContextId,
               SpecificationId = plan.SpecificationId,
               SpecificationVersion = plan.SpecificationVersion,
               CreatedAt = DateTimeOffset.UtcNow,
               CreatedBy = "PlanningEngine"
            }, cancellationToken);

            // Edge: 'consumes-context' (To the Downstream Boundary)
            await RecordEdgeAsync(new TraceabilityEdge
            {
               EdgeId = $"EDG-CSM-{Guid.NewGuid():N}",
               EdgeType = "consumes-context",
               SourceNodeId = context.ToBoundaryId,
               TargetNodeId = context.ConnectionContextId,
               SpecificationId = plan.SpecificationId,
               SpecificationVersion = plan.SpecificationVersion,
               CreatedAt = DateTimeOffset.UtcNow,
               CreatedBy = "PlanningEngine"
            }, cancellationToken);
         }
      }

      _logger.LogInformation("Boundary traceability initialization complete for Plan {PlanId}", plan.PlanId);
   }

}
