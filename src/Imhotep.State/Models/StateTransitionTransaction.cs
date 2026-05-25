using System;
using System.Collections.Generic;

namespace Imhotep.State.Models;

/// <summary>
/// ISL v2.2 Section 26.2: State Transition Transaction Schema.
/// Commits a formal state transition transaction across multiple state categories.
/// </summary>
public record StateTransitionTransaction
{
   public required string TransactionId { get; init; } = Guid.NewGuid().ToString();

   /// <summary>
   /// The type of transition transaction.
   /// </summary>
   public required string TransactionType { get; init; }

   /// <summary>
   /// The state records updated by this transaction.
   /// </summary>
   public required IReadOnlyList<string> AffectedStateRecords { get; init; }

   /// <summary>
   /// The events emitted by this transaction.
   /// </summary>
   public required IReadOnlyList<string> AffectedEventRecords { get; init; }

   /// <summary>
   /// Subsystem, task, user, or governance action initiating transition.
   /// </summary>
   public required string InitiatedBy { get; init; }

   public required DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

   public DateTimeOffset? CompletedAt { get; init; }

   /// <summary>
   /// Permitted values: pending, committed, failed, compensating, compensated.
   /// </summary>
   public required string Status { get; init; }

   /// <summary>
   /// Required when the transaction fails.
   /// </summary>
   public string? RecoveryAction { get; init; }
}
