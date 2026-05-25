using System;
using System.Collections.Generic;

namespace Imhotep.State.Models;

/// <summary>
/// ISL v2.2 Section 8.1: Base State Record Schema
/// Represents the current operational condition of a platform object [7].
/// </summary>
public record PlatformStateRecord
{
   public required string StateRecordId { get; init; } = Guid.NewGuid().ToString();
   public required string StateCategory { get; init; } // e.g., execution, artifact, governance
   public required string ObjectId { get; init; }
   public required string ObjectType { get; init; }
   public required string CurrentState { get; init; }
   public required string StateVersion { get; init; }
   public string? CorrelationId { get; init; }
   public required DateTimeOffset UpdatedAt { get; init; }
   public required string UpdatedBy { get; init; }
}

/// <summary>
/// ISL v2.2 Section 9.1: State Event Schema
/// Represents the immutable, chronological memory of how state evolved over time [2].
/// </summary>
public record StateEventRecord
{
   public required string StateEventId { get; init; } = Guid.NewGuid().ToString();
   public required string EventType { get; init; }
   public required string StateCategory { get; init; }
   public required string ObjectId { get; init; }
   public required string ObjectType { get; init; }
   public string? PriorState { get; init; }
   public string? NewState { get; init; }
   public required string CausedBy { get; init; }
   public string? CorrelationId { get; init; }
   public required DateTimeOffset EventTime { get; init; }
   public required int EventSequence { get; init; }
}

/// <summary>
/// ISL v2.2 Section 28.3: Snapshot Request Schema [3].
/// </summary>
public record StateSnapshotRequest
{
   public required string SnapshotType { get; init; } // execution-snapshot, artifact-snapshot, etc.
   public required IReadOnlyList<string> IncludedStateCategories { get; init; }
   public required string CreatedBy { get; init; }
   public required string StorageLocation { get; init; }
   public required bool RecoveryEligible { get; init; }
   public required string RetentionClass { get; init; }
}

/// <summary>
/// ISL v2.2 Section 29.4: Recovery Event Schema [8].
/// </summary>
public record RecoveryOutcomeRecord
{
   public required string RecoveryEventId { get; init; } = Guid.NewGuid().ToString();
   public required string RecoveryScope { get; init; }
   public required IReadOnlyList<string> AffectedObjectIds { get; init; }
   public required string ConsistencyStatus { get; init; } // consistent, inconsistent, corrupt
   public required string RecoveryOutcome { get; init; } // resume-safe, rollback-required, halt-required
   public required DateTimeOffset RecoveredAt { get; init; }
}

/// <summary>
/// ISL v1.2 Sec 15.3: Repair Record Schema.
/// Preserves the history of automated repair cycles to enforce termination limits.
/// </summary>
public record RepairRecord
{
   public required string RepairId { get; init; }
   public required string ArtifactId { get; init; }
   public required string TaskId { get; init; }
   public required string FailedValidationResultId { get; init; }

   /// <summary>
   /// The repair attempt number (e.g., 1, 2, 3) used to enforce the 5-attempt limit.
   /// </summary>
   public required int Iteration { get; init; }

   public string? ProposedCorrection { get; init; }
   public required string Outcome { get; init; } // resolved, unresolved, escalated, abandoned
   public required DateTimeOffset RecordedAt { get; init; }
}

/// <summary>
/// ISL v2.2 Sec 15.1: Task State Schema.
/// Records the current lifecycle status of construction tasks within a construction plan or execution graph.
/// </summary>
public record TaskStateRecord
{
   public required string TaskStateId { get; init; } = $"TSK-ST-{Guid.NewGuid():N}";
   public required string TaskId { get; init; }
   public required string PlanId { get; init; }

   public string? ExecutionGraphId { get; init; }
   public required string TaskType { get; init; }

   /// <summary>
   /// MUST be one of: pending, in-progress, completed, failed, escalated, skipped [1]
   /// </summary>
   public required string CurrentState { get; init; }

   public string? AssignedAgentRole { get; init; }
   public string? AssignedToolId { get; init; }

   // Track the artifacts, validations, and repairs tied to this specific task [1]
   public IReadOnlyList<string>? ProducedArtifactIds { get; init; }
   public IReadOnlyList<string>? ValidationResultIds { get; init; }
   public IReadOnlyList<string>? RepairRecordIds { get; init; }
   public IReadOnlyList<string>? GovernanceGateIds { get; init; }

   public DateTimeOffset? StartedAt { get; init; }
   public DateTimeOffset? CompletedAt { get; init; }
   public required DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// ISL v2.2 Sec 14.3 & ISL v1.2 Sec 21.2: Phase State Record.
/// Tracks the current status of the high-level execution phases.
/// </summary>
public record PhaseStateRecord
{
   public required string PhaseStateId { get; init; } = $"PHS-ST-{Guid.NewGuid():N}";

   /// <summary>
   /// Identifies the phase, e.g., "Artifact Generation", "Deterministic Validation", "Repair Cycles"
   /// </summary>
   public required string PhaseName { get; init; }

   /// <summary>
   /// MUST be one of: not-started, in-progress, completed, skipped, failed, escalated [2, 3]
   /// </summary>
   public required string CurrentState { get; init; }

   public DateTimeOffset? StartedAt { get; init; }
   public DateTimeOffset? CompletedAt { get; init; }
   public required DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
