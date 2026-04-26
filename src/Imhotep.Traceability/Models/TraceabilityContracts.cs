
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
/// Represents the result of an automated impact analysis when a specification changes,
/// identifying exactly what needs targeted reconstruction.
/// </summary>
public record ImpactAnalysisResult
{
   public required string ModifiedTraceabilityId { get; init; }

   /// <summary>
   /// The specific construction tasks that must be re-executed.
   /// </summary>
   public IReadOnlyList<string> ImpactedTaskIds { get; init; } = new List<string>();

   /// <summary>
   /// The specific artifacts that must be regenerated or updated.
   /// </summary>
   public IReadOnlyList<string> ImpactedArtifactIds { get; init; } = new List<string>();
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
/// A single node in the bidirectional Traceability Graph.
/// </summary>
public record TraceabilityNode
{
   public required string NodeId { get; init; } // The unique Traceability Identifier (e.g., REQ-001)
   public required NodeType Type { get; init; }
   public required string Description { get; init; }
   public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a bidirectional edge linking intent, execution, and implementation.
/// </summary>
public record TraceabilityEdge
{
   public required string SourceNodeId { get; init; }
   public required string TargetNodeId { get; init; }
   public required RelationshipType Relationship { get; init; }
   public required string TransactionId { get; init; } // Links the edge to the State/Memory execution context
}

#endregion
