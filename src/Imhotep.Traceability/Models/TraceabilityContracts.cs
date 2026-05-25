
using System;
using System.Collections.Generic;

namespace Imhotep.Traceability.Models;

#region -- Core Traceability Contracts --

/// <summary>
/// Represents a definitive, bidirectional link between a generated implementation artifact, 
/// the task that created it, and the original specification entity.
/// </summary>
public record ArtifactTraceabilityLink
{
   public required string LinkId { get; init; }

   /// <summary>
   /// The unique identifier of the physical file/artifact in the Artifact Repository.
   /// </summary>
   public required string ArtifactId { get; init; }

   /// <summary>
   /// The execution task that generated this artifact.
   /// </summary>
   public required string GeneratingTaskId { get; init; }

   /// <summary>
   /// The canonical entity (e.g., "REQ-001") that justified this artifact's creation.
   /// </summary>
   public required string SourceTraceabilityId { get; init; }

   public required DateTimeOffset RecordedAt { get; init; }
}

/// <summary>
/// Represents the persistent operational state of the platform to survive infrastructure failures [5].
/// </summary>
public record StateRecord
{
   public required string TransactionId { get; init; }
   public required string StateCategory { get; init; }
   public required string SerializedContext { get; init; }
   public required DateTimeOffset LastUpdated { get; init; }
}

#endregion
#region -- Traceability Graph Models --

/// <summary>
/// Represents the category of a node within the Traceability Graph.
/// </summary>
public enum NodeType
{
   SpecificationEntity, // e.g., "REQ-001", "POL-CJIS-001"
   ConstructionTask,    // e.g., "TASK-GEN-API-01"
   ReasoningAgent,      // e.g., "Implementation Generator"
   SoftwareArtifact,    // e.g., "IntakeController.cs"
   ValidationResult     // e.g., "VAL-RES-001"
}

/// <summary>
/// Represents the nature of the explicit edge creation between nodes.
/// </summary>
public enum RelationshipType
{
   Fulfills,        // Artifact -> Requirement
   GeneratedBy,     // Artifact -> Task/Agent
   Verifies,        // ValidationResult -> Policy/Requirement
   Constrains,      // Policy -> Service
   DependsOn        // Task -> Task
}

/// <summary>
/// ISL v1.4 Section 8.2: Traceability Node Base Schema.
/// Represents any lifecycle object (e.g., Artifact, Task, Boundary, Policy).
/// </summary>
public record TraceabilityNode
{
   public required string NodeId { get; init; }

   /// <summary>
   /// e.g., SpecificationEntity, ConstructionTask, Artifact, ValidationResult, 
   /// GovernanceEvent, ConstructionBoundary (per ISL v1.5 Sec 19.8)
   /// </summary>
   public required string NodeType { get; init; }

   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   public required string CreatedBy { get; init; }
   public required DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

   /// <summary>
   /// e.g., active, deprecated, superseded, failed, archived
   /// </summary>
   public required string Status { get; init; } = "active";

   public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// ISL v1.4 Section 9.2: Traceability Edge Base Schema.
/// The typed mathematical relationship between two nodes.
/// </summary>
public record TraceabilityEdge
{
   public required string EdgeId { get; init; } = $"EDG-{Guid.NewGuid():N}";

   /// <summary>
   /// e.g., originates, produces, implements, validates, governed-by, 
   /// produces-continuation (per ISL v1.5 Sec 19.8)
   /// </summary>
   public required string EdgeType { get; init; }

   public required string SourceNodeId { get; init; }
   public required string TargetNodeId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
   public required string CreatedBy { get; init; }

   public string? Rationale { get; init; } // Required when Edge is 'inferred'
   public string? Confidence { get; init; } // explicit, inferred, imported, repaired
}

/// <summary>
/// ISL v1.4 Section 18.2: Traceability Snapshot Schema.
/// A versioned view of the graph at a specific point in time (e.g., Boundary Handoff).
/// </summary>
public record TraceabilitySnapshot
{
   public required string SnapshotId { get; init; } = $"TRS-{Guid.NewGuid():N}";
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string CanonicalModelVersion { get; init; }
   public required DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
   public required string CreatedBy { get; init; }
   public required int NodeCount { get; init; }
   public required int EdgeCount { get; init; }

   /// <summary>
   /// readiness, execution-start, consolidation, deployment, audit, change-impact
   /// </summary>
   public required string SnapshotPurpose { get; init; }
   public required string StorageLocation { get; init; }
   public string? IntegrityHash { get; init; }
}

#endregion
