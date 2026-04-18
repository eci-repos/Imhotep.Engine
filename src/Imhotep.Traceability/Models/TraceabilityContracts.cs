
using System;
using System.Collections.Generic;

namespace Imhotep.Traceability.Models;

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

