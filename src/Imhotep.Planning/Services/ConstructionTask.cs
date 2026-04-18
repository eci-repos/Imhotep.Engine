namespace Imhotep.Planning.Services;

/// <summary>
/// Represents a discrete unit of work required to construct a part of the system.
/// </summary>
public record ConstructionTask
{
   /// <summary>
   /// The unique identifier for this specific task.
   /// </summary>
   public string TaskId { get; init; } = string.Empty;

   /// <summary>
   /// Categorizes the work (e.g., "Architecture", "Implementation", "Infrastructure", "Verification").
   /// </summary>
   public string TaskCategory { get; init; } = string.Empty;

   /// <summary>
   /// The TraceabilityId of the Canonical Entity this task is fulfilling (e.g., "SRV-INTAKE-01").
   /// </summary>
   public string TargetTraceabilityId { get; init; } = string.Empty;

   /// <summary>
   /// Details describing the work to be performed by the downstream agent.
   /// </summary>
   public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Defines the execution order by linking a predecessor task to a successor task.
/// </summary>
public record TaskDependency
{
   public string PredecessorTaskId { get; init; } = string.Empty;
   public string SuccessorTaskId { get; init; } = string.Empty;
}

/// <summary>
/// The operational blueprint representing the sequence of construction activities.
/// </summary>
public record ConstructionTaskGraph
{
   public IReadOnlyList<ConstructionTask> Tasks { get; init; } = new List<ConstructionTask>();
   public IReadOnlyList<TaskDependency> Dependencies { get; init; } = new List<TaskDependency>();
}
