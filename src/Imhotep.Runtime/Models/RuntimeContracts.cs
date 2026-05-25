
using System.Collections.Generic;
using Imhotep.Planning.Models;
using Imhotep.State.Models;

namespace Imhotep.Runtime.Models;

/// <summary>
/// Represents the final or intermediate outcome of a single construction task's execution.
/// </summary>
public record TaskExecutionResult
{
   /// <summary>
   /// The unique identifier of the task that was executed.
   /// </summary>
   public required string TaskId { get; init; }

   /// <summary>
   /// Indicates whether the task successfully passed both agent generation and deterministic tool validation.
   /// </summary>
   public required bool IsSuccessful { get; init; }

   /// <summary>
   /// The specific artifact paths or contents generated and verified during this task.
   /// </summary>
   public IReadOnlyList<string> GeneratedArtifacts { get; init; } = new List<string>();

   /// <summary>
   /// Detailed logs tracking the automated repair cycles required to achieve convergence.
   /// </summary>
   public IReadOnlyList<string> RepairCycleLogs { get; init; } = new List<string>();
}

/// <summary>
/// ISL v2.2 Sec 14.1: Execution State Schema.
/// Records the current condition of runtime execution, ensuring the platform can recover safely after interruption.
/// </summary>
public record ExecutionState
{
   // --- 1. Core Identity & Traceability [ISL v2.2 Sec 14.1] ---
   public required string ExecutionStateId { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string PlanId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   // --- 2. Execution Status [ISL v2.2 Sec 14.4] ---
   /// <summary>
   /// MUST be one of: pending, in-progress, completed, failed, escalated, halted, recovering
   /// </summary>
   public required string ExecutionStatus { get; init; }

   public string? CurrentPhase { get; init; }

   // --- 3. Structured Lifecycle Tracking [ISL v2.2 Sec 14.1] ---
   // Replaces the ad-hoc 'CompletedTasks' dictionary
   public required IReadOnlyList<PhaseStateRecord> PhaseStates { get; init; }
   public required IReadOnlyList<TaskStateRecord> TaskStates { get; init; }

   // --- 4. Active Invocations (Replaces 'ActiveTaskIds') ---
   public IReadOnlyList<string>? ActiveAgentInvocations { get; init; }
   public IReadOnlyList<string>? ActiveToolInvocations { get; init; }
   public IReadOnlyList<string>? ActiveRepairRecords { get; init; }

   // --- 5. Recovery & Completion ---
   public string? LastCheckpointId { get; init; }
   public string? CompletionReportId { get; init; }

   public required DateTimeOffset UpdatedAt { get; init; }
}

