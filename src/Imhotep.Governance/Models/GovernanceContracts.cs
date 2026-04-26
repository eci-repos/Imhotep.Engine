
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

