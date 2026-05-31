
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
/// ISL v1.7 Section 16.2: Governance Check Response Schema [1].
/// The deterministic command returned by the Governance Engine.
/// </summary>
public record GovernanceCheckResponse
{
   public required string CheckId { get; init; }

   /// <summary>
   /// allow, block, warn, escalate, approval-required, waiver-required, override-required [1].
   /// </summary>
   public required string Decision { get; init; }

   public IReadOnlyList<string>? ApplicablePolicies { get; init; }
   public string? RequiredGateId { get; init; }
   public string? RequiredRole { get; init; }
   public IReadOnlyList<string>? Findings { get; init; }
   public required string Rationale { get; init; }

   /// <summary>
   /// Expiration of the decision, if applicable (e.g., for time-bound waivers) [1].
   /// </summary>
   public DateTimeOffset? ExpiresAt { get; init; }

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

/// <summary>
/// Payload to request a time-bound governance exception (ISL v1.7 Sec 12.1).
/// </summary>
public record WaiverRequest
{
   public required string WaiverType { get; init; } // e.g., policy, validation, security, operational
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   // The specific policy, validation rule, or artifact being waived
   public required string TargetId { get; init; }

   public required string Justification { get; init; }

   // CRITICAL: Must define how the risk is mitigated while the waiver is active
   public required string CompensatingControls { get; init; }

   public required string RiskTier { get; init; }
   public required string RequestedBy { get; init; }

   // Waivers must have an expiration date
   public required DateTimeOffset Expiry { get; init; }
   public List<string> Evidence { get; init; } = new();
}

public record WaiverRecord
{
   public required string WaiverId { get; init; }
   public required string WaiverType { get; init; } // policy, validation, security, operational, deployment, traceability
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; }
   public required string Justification { get; init; }
   public required string CompensatingControls { get; init; }
   public required string RiskTier { get; init; }
   public required string RequestedBy { get; init; }
   public required string ApprovedBy { get; init; }
   public required DateTimeOffset ApprovedAt { get; init; }
   public required DateTimeOffset Expiry { get; init; } // Time-bound limit
   public required string Status { get; init; } // active, expired, revoked, closed
   public DateTimeOffset? ReviewDate { get; init; }
   public List<string> Evidence { get; init; } = new();
}

/// <summary>
/// Payload to request a privileged bypass of a failed automated check (ISL v1.7 Sec 13.1).
/// </summary>
public record OverrideRequest
{
   public required string OverrideType { get; init; } // e.g., readiness, validation, policy, deployment
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; }

   // The exact check or automated control that failed and is being bypassed
   public required string FailedControl { get; init; }

   public required string Justification { get; init; }
   public required string CompensatingControls { get; init; }

   public required string RequestedBy { get; init; }

   // Overrides must have an expiration timestamp
   public required DateTimeOffset Expiry { get; init; }
   public List<string> Evidence { get; init; } = new();
}

public record OverrideRecord
{
   public required string OverrideId { get; init; }
   public required string OverrideType { get; init; } // readiness, validation, policy, runtime, tool, model, deployment
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TargetId { get; init; }
   public required string FailedControl { get; init; }
   public required string Justification { get; init; }
   public required string CompensatingControls { get; init; }
   public required string RequestedBy { get; init; }
   public required string ApprovedBy { get; init; } // Override Authority
   public required DateTimeOffset ApprovedAt { get; init; }
   public required DateTimeOffset Expiry { get; init; }
   public required string Status { get; init; } // active, expired, revoked, closed
   public List<string> Evidence { get; init; } = new();
}

/// <summary>
/// Payload to request deployment authorization for release candidates (ISL v1.7 Sec 17.1).
/// </summary>
public record DeploymentAuthorizationRequest
{
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   public required string DeploymentTarget { get; init; }
   public required List<string> DeploymentArtifacts { get; init; } = new();

   public required string RiskTier { get; init; }

   // The evidence proving the artifacts are safe to deploy
   public required List<string> ValidationEvidence { get; init; } = new();
   public required List<string> PolicyEvidence { get; init; } = new();
   public required string TraceabilitySnapshotId { get; init; }

   public required string RequestedBy { get; init; }

   // Used if the authorization is intended to be time-bound
   public DateTimeOffset? RequestedExpiry { get; init; }
}

public record DeploymentAuthorizationRecord
{
   public required string DeploymentAuthorizationId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string DeploymentTarget { get; init; }
   public required List<string> DeploymentArtifacts { get; init; } = new();
   public required string RiskTier { get; init; }
   public required List<string> ValidationEvidence { get; init; } = new();
   public required List<string> PolicyEvidence { get; init; } = new();
   public required string TraceabilitySnapshotId { get; init; }
   public required string AuthorizedBy { get; init; } // Authorizing Official
   public required DateTimeOffset AuthorizedAt { get; init; }
   public DateTimeOffset? Expiry { get; init; }
   public required string Status { get; init; } // authorized, rejected, expired, revoked
}
