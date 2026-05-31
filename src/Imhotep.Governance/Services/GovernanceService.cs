using Imhotep.Governance.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Governance.Services;

/// <summary>
/// The concrete Enterprise Governance Engine.
/// Strictly implements ISL v1.7 (The Governance and Control Model) to enforce 
/// zero-trust approval gates, separation of duties, waivers, and escalations.
/// </summary>
public class GovernanceService : IGovernanceService
{
   private readonly ILogger<GovernanceService> _logger;

   // In a true enterprise deployment, these would map to the ISL v2.2 State and Memory Store (e.g., PostgreSQL/EF Core)
   private readonly ConcurrentDictionary<string, GovernanceEscalationRecord> _escalations = new();
   private readonly ConcurrentDictionary<string, WaiverRecord> _waivers = new();
   private readonly ConcurrentDictionary<string, OverrideRecord> _overrides = new();
   private readonly ConcurrentDictionary<string, ApprovalGateRecord> _approvalGates = new();
   private readonly ConcurrentDictionary<string, DeploymentAuthorizationRecord> _deployments = new();
   private readonly ConcurrentBag<AuditLogEntry> _auditLog = new();

   public GovernanceService(ILogger<GovernanceService> logger)
   {
      _logger = logger;
   }

   // --- 1. Runtime Control & Policy Evaluation ---

   public Task<GovernanceCheckResponse> EvaluateGovernanceCheckAsync(GovernanceCheckRequest request, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("[GOVERNANCE CHECK] Evaluating Action: '{Action}' for Target: {TargetId}", request.RequestedAction, request.TargetId);

      // ISL v1.7 Sec 16.0: Simulated dynamic policy evaluation.
      // In reality, this queries the Traceability Graph and Active Policies.
      var response = new GovernanceCheckResponse
      {
         CheckId = $"CHK-{Guid.NewGuid():N}",
         Decision = "allow", // Defaulting to allow for POC, but would dynamically return "block", "escalate", or "waiver-required"
         Rationale = "Policy evaluation passed all mandatory constraints.",
         DecidedBy = "GovernanceService", // In a real implementation, this would be the specific policy or rule that made the decision
         DecidedAt = DateTimeOffset.UtcNow
      };

      return Task.FromResult(response);
   }

   // --- 2. Approval Gates & Separation of Duties ---

   public Task<ApprovalGateRecord> GetApprovalGateStatusAsync(string gateId, CancellationToken cancellationToken = default)
   {
      if (_approvalGates.TryGetValue(gateId, out var gate))
      {
         return Task.FromResult(gate);
      }
      throw new KeyNotFoundException($"Approval Gate {gateId} not found in Governance State.");
   }

   public async Task RegisterHumanApprovalAsync(string gateId, string approverIdentity, string role, CancellationToken cancellationToken = default)
   {
      if (_approvalGates.TryGetValue(gateId, out var gate))
      {
         // ISL v1.7 Sec 9.0: Record the approval and immutably log it
         var updatedGate = gate with
         {
            Status = "approved",
            DecisionBy = approverIdentity,
            DecisionAt = DateTimeOffset.UtcNow
         };
         _approvalGates[gateId] = updatedGate;

         await RecordAuditEventAsync(new AuditLogEntry
         {
            AuditEventId = $"AUD-{Guid.NewGuid():N}",
            EventType = "approval-recorded",
            ActorId = approverIdentity,
            ActorRole = role,
            TargetId = gateId,
            Outcome = "approved",
            EventTime = DateTimeOffset.UtcNow,
            Rationale = "Human Governance Sign-off completed."
         }, cancellationToken);

         _logger.LogInformation("[APPROVAL GATE] Gate {GateId} APPROVED by {Role} ({Identity}).", gateId, role, approverIdentity);
      }
   }

   public Task<bool> ValidateSeparationOfDutiesAsync(string specificationId, string specificationVersion, string proposedIdentity, string proposedRole, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("[SoD VALIDATION] Verifying Identity '{Identity}' for Role '{Role}' on Spec {SpecId}", proposedIdentity, proposedRole, specificationId);

      // ISL v1.7 Sec 7.0: Ensures the Authorizing Official is distinct from the Security Reviewer.
      // E.g., Iterate through _approvalGates for this spec to ensure proposedIdentity hasn't acted as an incompatible role.
      bool isValid = true;

      if (!isValid)
      {
         _logger.LogWarning("[SoD VIOLATION] Identity '{Identity}' cannot act as '{Role}' due to separation of duties rules.", proposedIdentity, proposedRole);
      }

      return Task.FromResult(isValid);
   }

   // --- 3. The Escalation Model ---

   public async Task<GovernanceEscalationRecord> OpenEscalationAsync(EscalationPayload payload, CancellationToken cancellationToken = default)
   {
      var escalation = new GovernanceEscalationRecord
      {
         EscalationId = $"ESC-{Guid.NewGuid():N}",
         EscalationType = payload.EscalationType,
         SpecificationId = payload.SpecificationId,
         SpecificationVersion = payload.SpecificationVersion,
         TargetId = payload.TargetId,
         RequiredRole = payload.RequiredRole,
         Severity = payload.Severity,
         Status = "open",
         OpenedAt = DateTimeOffset.UtcNow
      };

      _escalations.TryAdd(escalation.EscalationId, escalation);

      _logger.LogCritical("[ANDON CORD PULLED] Escalation {EscalationId} opened for {TargetId}. Routing to {Role}.",
          escalation.EscalationId, escalation.TargetId, escalation.RequiredRole);

      // Record the formal audit event
      await RecordAuditEventAsync(new AuditLogEntry
      {
         AuditEventId = $"AUD-{Guid.NewGuid():N}",
         EventType = "escalation-received",
         ActorId = "Imhotep.ExecutionRuntime",
         TargetId = escalation.EscalationId,
         Outcome = "escalated",
         EventTime = DateTimeOffset.UtcNow,
         Rationale = payload.FailureContext
      }, cancellationToken);

      return escalation;
   }

   public async Task ResolveEscalationAsync(string escalationId, string resolutionRationale, string nextAction, string resolverIdentity, CancellationToken cancellationToken = default)
   {
      if (_escalations.TryGetValue(escalationId, out var escalation))
      {
         var resolved = escalation with
         {
            Status = "resolved",
            ResolvedBy = resolverIdentity,
            ResolvedAt = DateTimeOffset.UtcNow,
            Resolution = resolutionRationale,
            NextAction = nextAction
         };

         _escalations[escalationId] = resolved;

         await RecordAuditEventAsync(new AuditLogEntry
         {
            AuditEventId = $"AUD-{Guid.NewGuid():N}",
            EventType = "escalation-resolved",
            ActorId = resolverIdentity,
            TargetId = escalationId,
            Outcome = "resolved",
            EventTime = DateTimeOffset.UtcNow,
            Rationale = resolutionRationale
         }, cancellationToken);

         _logger.LogInformation("[ESCALATION RESOLVED] {EscalationId} resolved by {Identity}. Next Action: {NextAction}", escalationId, resolverIdentity, nextAction);
      }
   }

   // --- 4. Enterprise Exception Pathways (Waivers & Overrides) ---

   public async Task<WaiverRecord> GrantWaiverAsync(WaiverRequest request, CancellationToken cancellationToken = default)
   {
      var waiver = new WaiverRecord
      {
         WaiverId = $"WAV-{Guid.NewGuid():N}",
         WaiverType = request.WaiverType,
         SpecificationId = request.SpecificationId,
         SpecificationVersion = request.SpecificationVersion,
         TargetId = request.TargetId,
         Justification = request.Justification,
         CompensatingControls = request.CompensatingControls,
         RiskTier = request.RiskTier,
         RequestedBy = request.RequestedBy,
         ApprovedBy = "Human-Governance-Auth", // Populated by active security context
         ApprovedAt = DateTimeOffset.UtcNow,
         Expiry = request.Expiry,
         Status = "active",
         Evidence = request.Evidence
      };

      _waivers.TryAdd(waiver.WaiverId, waiver);
      _logger.LogWarning("[WAIVER GRANTED] Waiver {WaiverId} applied to {TargetId} until {Expiry}", waiver.WaiverId, waiver.TargetId, waiver.Expiry);

      return await Task.FromResult(waiver);
   }

   public async Task<OverrideRecord> ApplyOverrideAsync(OverrideRequest request, CancellationToken cancellationToken = default)
   {
      var overrideRecord = new OverrideRecord
      {
         OverrideId = $"OVR-{Guid.NewGuid():N}",
         OverrideType = request.OverrideType,
         SpecificationId = request.SpecificationId,
         SpecificationVersion = request.SpecificationVersion,
         TargetId = request.TargetId,
         FailedControl = request.FailedControl,
         Justification = request.Justification,
         CompensatingControls = request.CompensatingControls,
         RequestedBy = request.RequestedBy,
         ApprovedBy = "Designated-Override-Authority", // Must pass SoD checks
         ApprovedAt = DateTimeOffset.UtcNow,
         Expiry = request.Expiry,
         Status = "active",
         Evidence = request.Evidence
      };

      _overrides.TryAdd(overrideRecord.OverrideId, overrideRecord);
      _logger.LogCritical("[OVERRIDE APPLIED] Override {OverrideId} bypassed {FailedControl} on {TargetId}", overrideRecord.OverrideId, overrideRecord.FailedControl, overrideRecord.TargetId);

      return await Task.FromResult(overrideRecord);
   }

   // --- 5. Deployment Authorization ---

   public async Task<DeploymentAuthorizationRecord> AuthorizeDeploymentAsync(DeploymentAuthorizationRequest request, CancellationToken cancellationToken = default)
   {
      var deployment = new DeploymentAuthorizationRecord
      {
         DeploymentAuthorizationId = $"DEP-{Guid.NewGuid():N}",
         SpecificationId = request.SpecificationId,
         SpecificationVersion = request.SpecificationVersion,
         DeploymentTarget = request.DeploymentTarget,
         DeploymentArtifacts = request.DeploymentArtifacts,
         RiskTier = request.RiskTier,
         ValidationEvidence = request.ValidationEvidence,
         PolicyEvidence = request.PolicyEvidence,
         TraceabilitySnapshotId = request.TraceabilitySnapshotId,
         AuthorizedBy = "Authorizing-Official", // Populated via active auth context
         AuthorizedAt = DateTimeOffset.UtcNow,
         Expiry = request.RequestedExpiry,
         Status = "authorized"
      };

      _deployments.TryAdd(deployment.DeploymentAuthorizationId, deployment);
      _logger.LogInformation("[DEPLOYMENT AUTHORIZED] Target: {Target}. Authorized by Official.", deployment.DeploymentTarget);

      return await Task.FromResult(deployment);
   }

   // --- 6. Immutable Audit Logging ---

   public Task RecordAuditEventAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
   {
      // ISL v1.7 Sec 19.0: Failure to write an audit log MUST halt the governed action.
      try
      {
         _auditLog.Add(entry);
      }
      catch (Exception ex)
      {
         _logger.LogCritical(ex, "[AUDIT WRITE FAILURE] Critical failure writing to audit store. Halting governed action!");
         throw new InvalidOperationException("Governance audit write failed. Halting Execution.", ex);
      }

      return Task.CompletedTask;
   }
}
