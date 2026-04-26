using System.Collections.Generic;
using System.Linq;

namespace Imhotep.Planning.Models;

/// <summary>
/// Represents the classification of the task within the development lifecycle [3].
/// </summary>
public enum TaskCategory
{
   Architecture,    // Establishes structural elements
   Implementation,  // Generates executable code (e.g., .NET classes, REST APIs)
   Infrastructure,  // Defines deployment environments (e.g., Dockerfiles)
   Verification,    // Validates system behavior (e.g., unit tests, security scans)
   Integration      // Connects system components
}

public enum PlanTaskStatus
{
   Pending,
   InProgress,
   Completed,
   Failed,
   InRepair,
   Escalated
}

/// <summary>
/// Represents a single unit of work required to construct part of the system [5].
/// </summary>
public class ConstructionTask
{
   public string TaskId { get; set; }
   public TaskCategory Category { get; set; }

   /// <summary>
   /// The specific ISL Traceability ID this task fulfills (e.g., "ENT-NODS-CASE" or "SRV-INTAKE-01").
   /// </summary>
   public string TargetTraceabilityId { get; set; }

   public string Description { get; set; }
   public PlanTaskStatus ExecutionStatus { get; set; } = PlanTaskStatus.Pending;

   public int RepairAttempts { get; set; } = 0; // Tracks how many times this task has been sent for repair

   /// <summary>
   /// The specific cognitive agent role assigned to perform the reasoning or generation.
   /// </summary>
   public required string AssignedAgentRole { get; init; }

   /// <summary>
   /// A list of TaskIds that must successfully complete before this task can begin, forming the DAG.
   /// </summary>
   public HashSet<string> Dependencies { get; set; } = new HashSet<string>();

   public ConstructionTask(
      string taskId, TaskCategory category, string targetTraceabilityId, string description,
      string assignedAgentRole)
   {
      TaskId = taskId;
      Category = category;
      TargetTraceabilityId = targetTraceabilityId;
      Description = description;
      AssignedAgentRole = assignedAgentRole;
   }

   public ConstructionTask() { }
}

/// <summary>
/// Represents the construction process as a coordinated workflow (DAG) enabling parallel execution 
/// (ISL v1.5 Section 6.0).
/// </summary>
public record ConstructionTaskGraph
{
   public required string TransactionId { get; init; }
   public Dictionary<string, ConstructionTask> Tasks { get; init; } = new();
   public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
