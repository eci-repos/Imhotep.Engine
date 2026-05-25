using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Planning.Models;

public enum BoundaryType
{
   Foundation, Specification, Semantic, Planning, Runtime,
   Governance, Security, Tooling, Agent, Telemetry, Deployment, Experimental
}

public enum BoundaryStatus
{
   Pending, InProgress, Completed, Failed, Blocked, Escalated
}

public enum ConnectionContextType
{
   Contract
}

/// <summary>
/// ISL v1.5 Section 19.1: Boundary Definition
/// Defines a bounded planning, reasoning, validation, and execution scope.
/// </summary>
public record ConstructionBoundary
{
   public required string BoundaryId { get; init; }
   public required string BoundaryName { get; init; }
   public required string BoundaryPurpose { get; init; }

   /// <summary>
   /// e.g., foundation, semantic, planning, runtime, governance, security, tooling (ISL v1.5 Sec 19.2)
   /// </summary>
   public required BoundaryType BoundaryType { get; init; }

   public required IReadOnlyList<string> SourceEntityIds { get; init; }
   public required IReadOnlyList<string> TaskIds { get; init; }
   public IReadOnlyList<string>? ExpectedArtifactTypes { get; init; }

   public required IReadOnlyList<string> DependencyBoundaries { get; init; }
   public required IReadOnlyList<string> ConnectionContexts { get; init; }
   public required IReadOnlyList<string> EntryCriteria { get; init; }
   public required IReadOnlyList<string> ExitCriteria { get; init; }

   public string? ContinuationRecordId { get; init; }

   /// <summary>
   /// e.g., pending, in-progress, completed, failed, escalated
   /// </summary>
   public required BoundaryStatus Status { get; init; }
   public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// ISL v1.5 Section 19.4: Connection Context
/// Defines the permitted, required, and traceable interaction between construction boundaries.
/// </summary>
public record ConnectionContext
{
   public required string ConnectionContextId { get; init; }
   public required string FromBoundaryId { get; init; }
   public required string ToBoundaryId { get; init; }
   public required string ContextPurpose { get; init; }
   public required ConnectionContextType ContextType { get; init; }

   public required IReadOnlyList<string> ProvidedElements { get; init; }
   public required IReadOnlyList<string> RequiredElements { get; init; }

   public required string ValidationRule { get; init; }
   public required string TrustPolicy { get; init; }
   public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// ISL v1.5 Section 19.7: Boundary Continuation Records
/// Preserves the information required by downstream boundaries to continue construction.
/// </summary>
public record BoundaryContinuationRecord
{
   public required string ContinuationRecordId { get; init; }
   public required string BoundaryId { get; init; }
   public required DateTimeOffset CompletedAt { get; init; }

   public required IReadOnlyList<string> CompletedTaskIds { get; init; }
   public required IReadOnlyList<string> ProducedArtifactIds { get; init; }
   public required IReadOnlyList<string> DecisionRecordIds { get; init; }
   public required IReadOnlyList<string> ValidationResultIds { get; init; }

   public IReadOnlyList<string>? GovernanceRecordIds { get; init; }
   public required IReadOnlyList<string> OpenIssues { get; init; }

   public required string DownstreamContextSummary { get; init; }
   public IReadOnlyList<string>? NextBoundaryRecommendations { get; init; }
}

