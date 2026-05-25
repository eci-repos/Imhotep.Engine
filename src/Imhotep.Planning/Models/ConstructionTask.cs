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
   Integration,     // Connects system components

   Interpretation,
   Planning,
   Schema,
   Interface,
   Test,
   Security,
   Repair,
   Documentation,
   Consolidation,
   DeploymentPreparation,
   Traceability,
   Governance
}

public enum PlanStatus
{
   Draft, 
   Valid, 
   Executable, 
   InProgress, 
   Completed, 
   Failed, 
   Superseded,
   Pending,
   InRepair,
   Escalated,
   Skipped
}

// --- Core ISL v1.5 Enumerations ---
public enum PlanningMode { Advisory, Formal, Adaptation }
public enum TaskPriority { Critical, High, Medium, Low }
public enum DependencyType { Hard, Soft, Governance, Artifact, Validation, Repair, Traceability }

// --- ISL v1.5 Section 13.3: Dependency Record Schema ---
public record TaskDependencyRecord
{
   public required string DependencyId { get; init; } = $"DEP-{Guid.NewGuid()}";
   public required string SourceTaskId { get; init; }
   public required string TargetTaskId { get; init; }
   public required DependencyType DependencyType { get; init; }
   public required string Rationale { get; init; }
   public required bool Required { get; init; } = true ;
   public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// ISL v1.5 Section 17.2: Parallel Group Schema.
/// Defines tasks that are safe to execute concurrently.
/// </summary>
public record ParallelGroup
{
   public required string ParallelGroupId { get; init; }
   public required IReadOnlyList<string> TaskIds { get; init; }
   public required string DependencyBoundary { get; init; }
   public IReadOnlyList<string>? SharedResources { get; init; }
   public IReadOnlyList<string>? GovernanceConstraints { get; init; }
   public required string Rationale { get; init; }
}

/// <summary>
/// Represents a single unit of work required to construct part of the system [5].
/// </summary>
public record ConstructionTask
{
   public required string TaskId { get; init; } = $"TSK-{Guid.NewGuid()}";
   public required TaskCategory TaskType { get; init; }

   /// <summary>
   /// Specification entities or platform rules originating this task.
   /// </summary>
   public required IReadOnlyList<string> SourceEntityIds { get; init; }

   public required string Description { get; init; }

   /// <summary>
   /// Required when the task requires reasoning or generation.
   /// </summary>
   public string? AssignedAgentRole { get; init; }

   /// <summary>
   /// Tool capabilities required by the task (if deterministic).
   /// </summary>
   public IReadOnlyList<string>? RequiredToolCapabilities { get; init; }

   /// <summary>
   /// Task identifiers that MUST complete before this task begins.
   /// </summary>
   public required IReadOnlyList<string> Dependencies { get; init; }

   public IReadOnlyList<string>? ArtifactsProduced { get; init; }
   public IReadOnlyList<string>? ArtifactsConsumed { get; init; }
   public IReadOnlyList<string>? VerificationTasks { get; init; }
   public IReadOnlyList<string>? GovernanceConstraints { get; init; }

   public required TaskPriority Priority { get; init; } = TaskPriority.Medium;
   public required PlanStatus Status { get; init; } = PlanStatus.Pending;
   public required DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
   public DateTimeOffset? UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// ISL v1.5 Section 22.2: Planning Validation Report Schema.
/// Determines whether the generated plan is complete, internally consistent, 
/// dependency-safe, traceable, and executable.
/// </summary>
public record PlanningValidationReport
{
   public required string ValidationReportId { get; init; } = Guid.NewGuid().ToString();
   public required string PlanId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   /// <summary>
   /// passed, failed, warning
   /// </summary>
   public required string Outcome { get; init; }

   public IReadOnlyList<PlanningValidationFinding>? Findings { get; init; }
   public required DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
   public required string ValidatedBy { get; init; }

   /// <summary>
   /// A plan MUST NOT be marked executable unless all planning validation checks pass.
   /// </summary>
   public required bool Executable { get; init; }
}

/// <summary>
/// ISL v1.5 Section 22.3: Planning Validation Finding Schema.
/// </summary>
public record PlanningValidationFinding
{
   public required string FindingId { get; init; } = Guid.NewGuid().ToString();
   public required string FindingClass { get; init; }

   /// <summary>
   /// blocking, high, medium, low
   /// </summary>
   public required string Severity { get; init; }

   public string? TaskId { get; init; }
   public string? EntityId { get; init; }
   public required string Message { get; init; }
   public required string RequiredAction { get; init; }
}

/// <summary>
/// ISL v1.4 Sec 17.2: Impact Analysis Record Schema.
/// Defines the precise downstream impact of a specification change.
/// </summary>
public record ImpactAnalysisResult
{
   public required string AnalysisId { get; init; } = $"IAN-{Guid.NewGuid():N}";

   // The entity, change event, or governance event that triggered the analysis
   public required string TriggeredBy { get; init; }

   public required string SpecificationId { get; init; }
   public string? PreviousSpecificationVersion { get; init; }
   public required string NewSpecificationVersion { get; init; }

   // The core analysis collections dictating what the Execution Runtime must do
   public required IReadOnlyList<string> ChangedEntities { get; init; }
   public required IReadOnlyList<string> AffectedTasks { get; init; }
   public required IReadOnlyList<string> AffectedArtifacts { get; init; }

   // Optional collections based on the breadth of the impact
   public IReadOnlyList<string>? AffectedValidations { get; init; }
   public IReadOnlyList<string>? AffectedPolicies { get; init; }
   public IReadOnlyList<string>? AffectedDeploymentArtifacts { get; init; }

   // Explicitly tracking what is preserved to avoid full reconstruction
   public required IReadOnlyList<string> UnaffectedArtifacts { get; init; }

   // Audit and Traceability metadata
   public required string AnalysisMethod { get; init; }
   public required DateTimeOffset AnalysisTimestamp { get; init; } = DateTimeOffset.UtcNow;

   /// <summary>
   /// Must be one of: complete, partial, uncertain
   /// </summary>
   public required string Confidence { get; init; }

   public string? TraceabilitySnapshotId { get; init; }
}

/// <summary>
/// ISL v1.5 Sec 24.4: Plan Adaptation Record Schema.
/// Records how a construction plan was modified in response to a specification change.
/// </summary>
public record PlanAdaptationRecord
{
   public required string AdaptationId { get; init; } = $"ADP-{Guid.NewGuid():N}";
   public required string PriorPlanId { get; init; }
   public required string NewPlanId { get; init; }

   public required string PreviousSpecificationVersion { get; init; }
   public required string NewSpecificationVersion { get; init; }
   public required string ImpactAnalysisId { get; init; }

   public required IReadOnlyList<string> AffectedTaskIds { get; init; }
   public required IReadOnlyList<string> PreservedTaskIds { get; init; }
   public IReadOnlyList<string>? AddedTaskIds { get; init; }
   public IReadOnlyList<string>? RemovedTaskIds { get; init; }
   public IReadOnlyList<string>? ResetTaskIds { get; init; }

   public required DateTimeOffset AdaptedAt { get; init; } = DateTimeOffset.UtcNow;

   /// <summary>
   /// adapted, failed, escalated
   /// </summary>
   public required string Outcome { get; init; }
}

/// <summary>
/// Represents the construction process as a coordinated workflow (DAG) enabling parallel execution 
/// (ISL v1.5 Section 14.2).
/// </summary>
public record ConstructionTaskGraph
{
   public required string PlanId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string CanonicalModelVersion { get; init; }
   public required string ReadinessLevel { get; init; }
   public required PlanningMode PlanningMode { get; init; }
   public required DateTimeOffset CreatedAt { get; init; }
   public DateTimeOffset? UpdatedAt { get; init; }
   public required PlanStatus Status { get; init; }

   // Tasks and dependencies must be explicit read-only lists, not a Dictionary
   public required IReadOnlyList<ConstructionTask> Tasks { get; init; }
   public required IReadOnlyList<TaskDependencyRecord> Dependencies { get; init; }
   public required IReadOnlyList<string> CriticalPath { get; init; }
   public required IReadOnlyList<ParallelGroup> ParallelGroups { get; init; }

   public object? VerificationPlan { get; init; }
   public object? ArtifactProductionPlan { get; init; }
   public IReadOnlyList<string>? GovernanceConstraints { get; init; }
   public string? TraceabilitySnapshotId { get; init; }

   /// <summary>
   /// The explicit boundaries organizing the task graph into encapsulated scopes of work.
   /// </summary>
   public IReadOnlyList<ConstructionBoundary>? Boundaries { get; init; }

   /// <summary>
   /// The strict communication contracts governing artifact and context sharing between boundaries.
   /// </summary>
   public IReadOnlyList<ConnectionContext>? ConnectionContexts { get; init; }
}

public record ExecutionHandoffRecord
{
   public required string HandoffId { get; init; } = $"HND-{Guid.NewGuid():N}";
   public required string PlanId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string ReadinessLevel { get; init; }
   public required string ValidationReportId { get; init; }
   public required string TraceabilitySnapshotId { get; init; }
   public required DateTimeOffset HandedOffAt { get; init; }
   public required bool AcceptedByRuntime { get; init; }
   public string? RejectionReason { get; init; }
}

/// <summary>
/// ISL v3.0 Sec 26.0: The formal output contract for plan generation.
/// Encapsulates the generated graph, its validation evidence, and the formal execution handoff.
/// </summary>
public record PlanGenerationResult
{
   public required ConstructionTaskGraph TaskGraph { get; init; }
   public required PlanningValidationReport ValidationReport { get; init; }
   public required ExecutionHandoffRecord HandoffRecord { get; init; }
}
