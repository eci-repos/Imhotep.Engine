
using System.Collections.Generic;
using Imhotep.Planning.Models;

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
/// Reflects the real-time status of task execution within the construction engine.
/// This state allows the runtime to recover from interruptions and resume construction reliably.
/// </summary>
public record ExecutionState
{
   /// <summary>
   /// The Transaction ID of the active specification blueprint.
   /// </summary>
   public required string TransactionId { get; init; }

   /// <summary>
   /// The tasks currently being processed by the agent orchestrator or tool plugins.
   /// </summary>
   public IReadOnlyList<string> ActiveTaskIds { get; init; } = new List<string>();

   /// <summary>
   /// A historical record of all successfully completed tasks and their verified outcomes.
   /// </summary>
   public IReadOnlyDictionary<string, TaskExecutionResult> CompletedTasks { get; init; } = new Dictionary<string, TaskExecutionResult>();

   /// <summary>
   /// Indicates whether the entire autonomous construction loop has reached stable convergence.
   /// </summary>
   public bool IsWorkflowComplete { get; init; }
}

