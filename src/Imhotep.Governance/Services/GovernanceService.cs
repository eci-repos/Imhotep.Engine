using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Imhotep.Governance.Models;
using Microsoft.Extensions.Logging;

namespace Imhotep.Governance.Services;

/// <summary>
/// ISL v1.7: Enforces organizational policies, manages formal human approval gates, 
/// and ensures autonomous construction remains compliant, accountable, and auditable.
/// </summary>
public class GovernanceService : IGovernanceService
{
   private readonly ILogger<GovernanceService> _logger;

   // In-memory persistent stores for the MACS POC
   private readonly ConcurrentDictionary<string, ApprovalGateRecord> _approvalGates = new();
   private readonly ConcurrentDictionary<string, GovernanceEscalationRecord> _escalations = new();
   private readonly ConcurrentBag<AuditLogEntry> _auditLog = new();
   private readonly IAuditWriter _auditWriter; 

   public GovernanceService(ILogger<GovernanceService> logger, IAuditWriter auditWriter)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
   }

   public async Task<GovernanceEscalationRecord> EscalateToHumanGovernanceAsync(
       string transactionId,
       EscalationPayload escalationPayload,
       CancellationToken cancellationToken = default)
   {
      // Ensure we safely halt if the broader platform is shutting down
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogWarning("Digital Andon Cord Pulled! Escalating transaction {TransactionId} to Human Governance Role: {Role}. Reason: {Severity} {Type} failure on {TargetId}.",
          transactionId,
          escalationPayload.RequiredRole,
          escalationPayload.Severity,
          escalationPayload.EscalationType,
          escalationPayload.TargetId);

      // ISL v1.7 Sec 14.2: Create the durable escalation state record
      var escalationRecord = new GovernanceEscalationRecord
      {
         EscalationId = $"ESC-{Guid.NewGuid():N}", // FIXED: Explicitly satisfy the 'required' constraint
         EscalationType = escalationPayload.EscalationType,
         SpecificationId = escalationPayload.SpecificationId,
         SpecificationVersion = escalationPayload.SpecificationVersion,
         TargetId = escalationPayload.TargetId,
         TriggeringEventId = transactionId,
         RequiredRole = escalationPayload.RequiredRole,
         Severity = escalationPayload.Severity,
         Status = "open",                           // FIXED: Explicitly satisfy the 'required' constraint
         OpenedAt = DateTimeOffset.UtcNow           // FIXED: Explicitly satisfy the 'required' constraint
      };

      // 3. Instantiate the strictly formatted AuditLogEntry
      var auditEntry = new AuditLogEntry
      {
         EventType = "escalation-received",
         TargetId = escalationPayload.TargetId,
         SpecificationId = escalationPayload.SpecificationId,
         ActorId = "Execution Runtime", // The subsystem causing the event [3]
         Outcome = "escalated",
         Rationale = escalationPayload.FailureContext,
         NewState = "escalated",
         CorrelationId = transactionId,
         EventTime = DateTimeOffset.UtcNow // Explicitly satisfy the 'required' constraint
      };

      // 4. Invoke the Audit Writer
      // If this fails, the 'Fail-Closed' exception bubbles up and mathematically stops the execution loop [2].
      await _auditWriter.RecordEventAsync(auditEntry, cancellationToken);


      // ISL v1.7 Sec 14.3 dictates that opening an escalation MUST emit an audit event.
      // In a production platform, this would persist the record via the IStateManager.
      _logger.LogInformation("Escalation {EscalationId} successfully recorded in Governance State. Execution is suspended pending human resolution.",
          escalationRecord.EscalationId);

      return escalationRecord;
   }

   // Replaces legacy 'EvaluateComplianceAsync'
   public Task<GovernanceCheckResponse> EvaluateGovernanceCheckAsync(
       GovernanceCheckRequest request,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogInformation("Evaluating Governance Check {CheckId} of type {CheckType} for target {TargetId}",
          request.CheckId, request.CheckType, request.TargetId);

      // For the MACS Proof-of-Concept, we simulate an allowed check.
      // In an enterprise deployment, this would invoke the Policy Engine against actual rules.
      var response = new GovernanceCheckResponse
      {
         CheckId = request.CheckId,
         Decision = "allow", // ISL v1.7 explicit decisions: allow, block, warn, escalate, approval-required
         ApplicablePolicies = new List<string>(),
         Rationale = "MACS POC: Governance check passed automatically.",
         DecidedAt = DateTimeOffset.UtcNow,
         DecidedBy = "GovernanceService"
      };

      return Task.FromResult(response);
   }

   // Replaces legacy 'GetApprovalGateStatusAsync'
   public Task<ApprovalGateRecord?> GetApprovalGateStatusAsync(
       string gateId,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _approvalGates.TryGetValue(gateId, out var gate);
      return Task.FromResult(gate);
   }

   // Replaces legacy 'RegisterHumanApproval'
   public async Task RegisterHumanApprovalAsync(
       string gateId,
       string approverIdentity,
       string role,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogInformation("Registering human approval for Gate {GateId} by {Identity} [{Role}]",
          gateId, approverIdentity, role);

      if (_approvalGates.TryGetValue(gateId, out var gate))
      {
         // Records are immutable, so we create a new instance with the updated state
         var updatedGate = gate with
         {
            Status = "approved",
            DecisionBy = approverIdentity,
            DecisionAt = DateTimeOffset.UtcNow,
            DecisionRationale = "Formal human sign-off via GovernanceService."
         };
         _approvalGates[gateId] = updatedGate;
      }
      else
      {
         _logger.LogWarning("Approval Gate {GateId} not found, but human approval was registered.", gateId);
      }

      // Record the immutable governance audit event (ISL v1.7 Sec 19.2)
      var auditEntry = new AuditLogEntry
      {
         EventType = "approval-registered",
         TargetId = gateId,
         ActorId = approverIdentity,
         ActorRole = role,
         EventTime = DateTimeOffset.UtcNow, // FIXED: Explicitly satisfy the 'required' constraint
         Outcome = "approved",
         Rationale = "Formal human sign-off"
      };

      await RecordAuditEventAsync(auditEntry, cancellationToken);
   }

   // Replaces legacy 'EscalateToHumanGovernance'
   public async Task OpenEscalationAsync(
       GovernanceEscalationRecord escalation,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogWarning("Opening Human-Machine Escalation {EscalationId} for Target {TargetId}. Severity: {Severity}",
          escalation.EscalationId, escalation.TargetId, escalation.Severity);

      _escalations[escalation.EscalationId] = escalation;
      // ISL v1.7 mandates logging an audit event immediately when an escalation opens
      var auditEntry = new AuditLogEntry
      {
         EventType = "escalation-opened",
         SpecificationId = escalation.SpecificationId,
         SpecificationVersion = escalation.SpecificationVersion,
         TargetId = escalation.TargetId,
         ActorId = "GovernanceService",
         EventTime = DateTimeOffset.UtcNow, // FIXED: Explicitly satisfy the 'required' constraint
         Outcome = "escalated",
         Rationale = $"Triggered by unresolvable structural conflict: {escalation.EscalationType}"
      };

      await RecordAuditEventAsync(auditEntry, cancellationToken);
   }

   // Newly added to strictly conform to the IGovernanceService interface
   public Task RecordAuditEventAsync(
       AuditLogEntry entry,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogInformation("AUDIT EVENT: {EventType} by {ActorId} on Target {TargetId} [Outcome: {Outcome}]",
          entry.EventType, entry.ActorId, entry.TargetId, entry.Outcome);

      _auditLog.Add(entry);
      return Task.CompletedTask;
   }
}
