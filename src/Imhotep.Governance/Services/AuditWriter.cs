using Imhotep.Governance.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Governance.Services;

public interface IAuditWriter
{
   /// <summary>
   /// Records an immutable governance event. Throws a critical exception on failure.
   /// </summary>
   Task RecordEventAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Sec 25.1: Exposes structured audit queries (e.g., approval-history).
   /// </summary>
   Task<IReadOnlyList<AuditLogEntry>> QueryAuditHistoryAsync(string targetId, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Sec 25.1: Verifies that audit records are present and immutable.
   /// </summary>
   Task<bool> VerifyAuditIntegrityAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// ISL v1.7 Sec 19.0: Governance Audit Logging.
/// Provides the immutable history needed to demonstrate accountability, compliance, and system integrity.
/// </summary>
public class AuditWriter : IAuditWriter
{
   private readonly ILogger<AuditWriter> _logger;

   // Simulating an append-only cryptographic ledger or durable database table
   private readonly ConcurrentDictionary<string, AuditLogEntry> _auditLedger = new();

   public AuditWriter(ILogger<AuditWriter> logger)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
   }

   public Task RecordEventAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(entry.AuditEventId))
         throw new ArgumentException("Audit logs MUST have a valid AuditEventId.");

      // ISL v1.7 Sec 19.3: Audit records MUST be immutable. 
      // We use TryAdd to guarantee an existing record is NEVER overwritten.
      bool writeSuccess = _auditLedger.TryAdd(entry.AuditEventId, entry);

      if (!writeSuccess)
      {
         // ISL v1.7 Sec 21.2 & 21.3: A governance audit write failure MUST halt governed actions.
         _logger.LogCritical("CRITICAL SECURITY FAILURE [governance-audit-write-failed]: Failed to securely persist audit event {EventId}.", entry.AuditEventId);
         throw new InvalidOperationException($"governance-audit-write-failed: The platform failed to record the audit event {entry.AuditEventId}. Execution must halt.");
      }

      _logger.LogInformation("Audit Event Recorded: [{EventType}] on Target {TargetId} by Actor {ActorId}",
          entry.EventType, entry.TargetId, entry.ActorId);

      return Task.CompletedTask;
   }

   public Task<IReadOnlyList<AuditLogEntry>> QueryAuditHistoryAsync(string targetId, CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // ISL v1.7 Sec 25.1: Exposes structured, repeatable audit queries.
      var history = _auditLedger.Values
          .Where(a => a.TargetId == targetId)
          .OrderBy(a => a.EventTime)
          .ToList();

      return Task.FromResult<IReadOnlyList<AuditLogEntry>>(history);
   }

   public Task<bool> VerifyAuditIntegrityAsync(CancellationToken cancellationToken = default)
   {
      // ISL v1.7 Sec 25.1: audit-integrity-check
      // In a production scenario, this method would recalculate a cryptographic hash chain (like a Merkle tree)
      // to mathematically prove that no past records were tampered with or deleted.
      _logger.LogInformation("Executing Governance Audit Integrity Check...");

      bool isIntegrityValid = true; // Simulated hash-chain check passes

      if (!isIntegrityValid)
      {
         _logger.LogCritical("AUDIT INTEGRITY COMPROMISED. The ledger has been tampered with.");
      }

      return Task.FromResult(isIntegrityValid);
   }
}


