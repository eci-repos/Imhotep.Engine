
using System;
using System.Collections.Generic;

namespace Imhotep.Governance.Models;

/// <summary>
/// Represents a formal business, security, or regulatory rule that constrains the autonomous system.
/// </summary>
public record GovernancePolicy
{
   /// <summary>
   /// The TraceabilityId of the policy constraint (e.g., "POL-CJIS-001").
   /// </summary>
   public required string PolicyId { get; init; }

   /// <summary>
   /// Describes the security or operational constraint.
   /// </summary>
   public string Description { get; init; } = string.Empty;

   /// <summary>
   /// The enforcement tier of the policy (e.g., "Mandatory", "Recommended", "Optional").
   /// </summary>
   public required string ComplianceTier { get; init; }
}

/// <summary>
/// Represents a formal human checkpoint required before the specification can advance 
/// to the Machine-Valid or Autonomous-Ready level.
/// </summary>
public record ApprovalGate
{
   public required string GateId { get; init; }
   public required string TransactionId { get; init; }

   /// <summary>
   /// The human governance role required to clear this gate (e.g., "Security Validator", "Court Auditor").
   /// </summary>
   public required string RequiredRole { get; init; }

   /// <summary>
   /// The current state of the gate (e.g., "Pending", "Approved", "Escalated").
   /// </summary>
   public string Status { get; set; } = "Pending";

   /// <summary>
   /// Captures the exact timestamp of human sign-off.
   /// </summary>
   public DateTimeOffset? ApprovedAt { get; set; }

   /// <summary>
   /// Captures the identity of the human authority who granted the approval.
   /// </summary>
   public string ApprovedBy { get; set; } = string.Empty;
}

// -----------------------------------------------------------------------------
// The following records represent the core data contracts for the Governance
// Engine, including the schemas for governance checks, approval gates,
// escalations, and audit logs. These contracts are designed to be extensible
// and adaptable to various enterprise governance scenarios while adhering
// to the ISL v1.7 specifications.

/// <summary>
/// ISL v1.7 Section 16.1: Governance Check Request Schema [3].
/// </summary>
public record GovernanceCheckRequest
{
   public required string CheckId { get; init; } = $"GOV-CHK-{Guid.NewGuid():N}";
   public required string CheckType { get; init; } // e.g., "execution", "planning", "tool", "model"
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; }
   public required string TargetType { get; init; } // e.g., "task", "artifact", "tool-invocation"
   public required string RequestedAction { get; init; } // e.g., "dispatch", "promote"
   public required DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
   public required string RequestedBy { get; init; }
}

/// <summary>
/// ISL v1.7 Section 16.2: Governance Check Response Schema [4].
/// </summary>
public record GovernanceCheckResponse
{
   public required string CheckId { get; init; }
   public required string Decision { get; init; } // e.g., allow, block, warn, escalate, approval-required
   public IReadOnlyList<string>? ApplicablePolicies { get; init; }
   public string? RequiredGateId { get; init; }
   public string? RequiredRole { get; init; }
   public IReadOnlyList<string>? Findings { get; init; }
   public required string Rationale { get; init; }
   public required DateTimeOffset DecidedAt { get; init; }
   public required string DecidedBy { get; init; }
}

/// <summary>
/// ISL v1.7 Section 9.2: Approval Gate Record Schema [1].
/// </summary>
public record ApprovalGateRecord
{
   public required string GateId { get; init; }
   public required string GateType { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public string? TargetId { get; init; }
   public required string RequiredRole { get; init; }
   public required string RequestedBy { get; init; }
   public required DateTimeOffset RequestedAt { get; init; }
   public required string Status { get; init; } // pending, approved, rejected, expired, cancelled
   public string? DecisionBy { get; init; }
   public DateTimeOffset? DecisionAt { get; init; }
   public string? DecisionRationale { get; init; }
}

/// <summary>
/// ISL v1.7: The machine-assembled context drop sent to Human Governance Roles.
/// Acts as the ultimate digital "Andon Cord" [3].
/// </summary>
public record EscalationPayload
{
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; } // Task, artifact, validation, or policy affected
   public required string EscalationType { get; init; } // e.g., repair, policy, approval, traceability
   public required string RequiredRole { get; init; } // e.g., "Security Validator", "IT Architect"
   public required string Severity { get; init; } // blocking, high, medium, low

   /// <summary>
   /// A structured log of the failed deterministic validation or context failure [5].
   /// </summary>
   public required string FailureContext { get; init; }

   /// <summary>
   /// The exact bidirectional graph link mapping the failure back to the specification [5].
   /// </summary>
   public required IReadOnlyList<string> TraceabilityPath { get; init; }

   /// <summary>
   /// The full memory of the attempted reasoning and artifact generation steps prior to escalation [5].
   /// </summary>
   public IReadOnlyList<string>? RepairHistory { get; init; }
}

/// <summary>
/// ISL v1.7 Section 14.2: Escalation Record Schema.
/// The durable governance state record tracking the open escalation [6].
/// </summary>
public record GovernanceEscalationRecord
{
   public required string EscalationId { get; init; } = $"ESC-{Guid.NewGuid():N}";
   public required string EscalationType { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; }
   public string? TriggeringEventId { get; init; }
   public required string RequiredRole { get; init; }
   public required string Severity { get; init; }

   /// <summary>
   /// open, resolved, rejected, expired, cancelled
   /// </summary>
   public required string Status { get; init; } = "open";
   public required DateTimeOffset OpenedAt { get; init; } = DateTimeOffset.UtcNow;

   public string? ResolvedBy { get; init; }
   public DateTimeOffset? ResolvedAt { get; init; }
   public string? Resolution { get; init; }

   /// <summary>
   /// resume, repair, replan, waive, override, halt, fail
   /// </summary>
   public string? NextAction { get; init; }
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
