using System;
using System.Collections.Generic;

namespace Imhotep.Governance.Models;

/// <summary>
/// ISL v1.7 Section 19.2: Audit Log Entry Schema.
/// Represents an immutable governance or lifecycle event record for enterprise auditability.
/// </summary>
public record AuditLogEntry
{
   /// <summary>
   /// Unique audit event identifier.
   /// </summary>
   public string AuditEventId { get; init; } = $"AUD-{Guid.NewGuid():N}";

   /// <summary>
   /// The type of audit event (e.g., approval-requested, policy-evaluated, escalation-resolved).
   /// </summary>
   public required string EventType { get; init; }

   /// <summary>
   /// Required when the event is specification-scoped.
   /// </summary>
   public string? SpecificationId { get; init; }

   /// <summary>
   /// Required when the event is specification-version scoped.
   /// </summary>
   public string? SpecificationVersion { get; init; }

   /// <summary>
   /// The entity, task, artifact, policy, gate, waiver, override, or deployment target affected.
   /// </summary>
   public string? TargetId { get; init; }

   /// <summary>
   /// The user, role, component, or system causing the event.
   /// </summary>
   public required string ActorId { get; init; }

   /// <summary>
   /// The governance role used during the event (if applicable).
   /// </summary>
   public string? ActorRole { get; init; }

   /// <summary>
   /// The exact time the event occurred.
   /// </summary>
   public required DateTimeOffset EventTime { get; init; } = DateTimeOffset.UtcNow;

   /// <summary>
   /// The event outcome (e.g., approved, rejected, escalated).
   /// </summary>
   public string? Outcome { get; init; }

   /// <summary>
   /// Required for approvals, rejections, waivers, overrides, and deployment decisions.
   /// </summary>
   public string? Rationale { get; init; }

   /// <summary>
   /// Evidence references supporting the event or decision.
   /// </summary>
   public IReadOnlyList<string>? Evidence { get; init; }

   /// <summary>
   /// The state of the target before the event.
   /// </summary>
   public string? PriorState { get; init; }

   /// <summary>
   /// The state of the target after the event.
   /// </summary>
   public string? NewState { get; init; }

   /// <summary>
   /// Related workflow, execution graph, or governance check identifier.
   /// </summary>
   public string? CorrelationId { get; init; }
}
