using System;
using System.Collections.Generic;

namespace Imhotep.Contracts.Governance;

/// <summary>
/// ISL v1.3 Sec 6.0: Readiness Level Overview.
/// Defines the strict progression from active authoring to autonomous construction authorization.
/// </summary>
public enum ReadinessLevel
{
   /// <summary>
   /// Exploratory stage. The platform provides advisory assistance but does not construct the system [8].
   /// </summary>
   Draft,

   /// <summary>
   /// The blueprint is structurally defined and ready for evaluation by Human Governance Roles [9].
   /// </summary>
   Reviewable,

   /// <summary>
   /// The blueprint has passed schema validation and is normalized into the canonical semantic model [10].
   /// </summary>
   MachineValid,

   /// <summary>
   /// All Approval Gates are cleared. The platform is officially authorized to begin autonomous construction [11].
   /// </summary>
   AutonomousReady
}

/// <summary>
/// ISL v1.3 Sec 14.1: Readiness Report Schema (Mapped as ReadinessStatus).
/// Acts as a cross-boundary contract passed to Planning and Execution engines.
/// </summary>
public record ReadinessStatus
{
   public required string ReportId { get; init; } = $"RR-{Guid.NewGuid():N}";
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   public required ReadinessLevel Level { get; init; }
   public ReadinessLevel? TargetLevel { get; init; }

   /// <summary>
   /// MUST be one of: passed, failed, blocked, override-required
   /// </summary>
   public required string EvaluationOutcome { get; init; }

   public required IReadOnlyList<CriterionResult> CriteriaResults { get; init; }
   public IReadOnlyList<string> Exceptions { get; init; } = new List<string>().AsReadOnly();
   public IReadOnlyList<string>? Warnings { get; init; }
   public required IReadOnlyList<string> EvidenceRecords { get; init; }

   public required DateTimeOffset EvaluatedAt { get; init; }
   public required string EvaluatedBy { get; init; }
}

/// <summary>
/// ISL v1.3 Sec 14.2: Criterion Result Schema.
/// Tracks the exact evaluation outcome of individual readiness rules.
/// </summary>
public record CriterionResult
{
   public required string CriterionId { get; init; }
   public required string Description { get; init; }

   /// <summary>
   /// MUST be one of: passed, failed, not-applicable, not-evaluated
   /// </summary>
   public required string Result { get; init; }

   public required string VerificationMethod { get; init; }
   public string? Evidence { get; init; }
   public required bool Blocking { get; init; }
   public string? Remediation { get; init; }
}
