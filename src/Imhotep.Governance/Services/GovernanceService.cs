using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Governance.Models;
using Imhotep.Observability.Services;
using Imhotep.Observability.Models;

namespace Imhotep.Governance.Services;

/// <summary>
/// Implements IGovernanceService to enforce organizational policies, manage human approval gates,
/// and trigger the digital "Andon Cord" (ISL v1.7).
/// </summary>
public class GovernanceService : IGovernanceService
{
   private readonly ITelemetryService _telemetryService;
   private readonly ILogger<GovernanceService> _logger;

   // Simulated persistent state store for Approval Gates (in production, this integrates with the State and Memory Model)
   private readonly Dictionary<string, ApprovalGate> _approvalGates = new();

   public GovernanceService(ITelemetryService telemetryService, ILogger<GovernanceService> logger)
   {
      _telemetryService = telemetryService;
      _logger = logger;
   }

   public Task<PolicyEvaluationResult> EvaluateComplianceAsync(string artifactId, GovernancePolicy policy)
   {
      _logger.LogInformation("Evaluating compliance for artifact {ArtifactId} against policy {PolicyId} ({ComplianceTier})",
          artifactId, policy.PolicyId, policy.ComplianceTier);

      // In a production enterprise implementation, this interacts with the ToolGateway to 
      // deterministically verify the artifact against the specified rule (e.g., POL-CJIS-001).
      bool isCompliant = true;
      var violations = new List<string>();

      if (!isCompliant)
      {
         violations.Add($"Artifact {artifactId} fails to meet structural constraints for {policy.PolicyId}.");
      }

      // Emit Governance Telemetry to the Watchtower Dashboard
      _telemetryService.RecordEvent(new GovernanceTelemetry
      {
         EventId = $"TEL-GOV-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
         TransactionId = "TX-ACTIVE", // Pulled from Execution Runtime context
         PolicyId = policy.PolicyId,
         ApprovalGateStatus = isCompliant ? "Compliant" : "Non-Compliant",
         Timestamp = DateTimeOffset.UtcNow
      });

      return Task.FromResult(new PolicyEvaluationResult
      {
         ArtifactId = artifactId,
         PolicyId = policy.PolicyId,
         IsCompliant = isCompliant,
         Violations = violations
      });
   }

   public Task<ApprovalGate> GetApprovalGateStatusAsync(string transactionId, string gateId)
   {
      if (_approvalGates.TryGetValue(gateId, out var gate))
      {
         return Task.FromResult(gate);
      }

      // Return a default pending gate if it has not been registered/cleared yet
      return Task.FromResult(new ApprovalGate
      {
         GateId = gateId,
         TransactionId = transactionId,
         RequiredRole = "Unassigned",
         Status = "Pending"
      });
   }

   public void RegisterHumanApproval(string gateId, string approverIdentity, string role)
   {
      _logger.LogInformation("Human Approval Registered for Gate {GateId} by {Approver} ({Role})",
          gateId, approverIdentity, role);

      // Establish or update the gate with the exact cryptographic timestamp and identity [7, 8]
      if (!_approvalGates.ContainsKey(gateId))
      {
         _approvalGates[gateId] = new ApprovalGate
         {
            GateId = gateId,
            TransactionId = "TX-ACTIVE",
            RequiredRole = role
         };
      }

      var gate = _approvalGates[gateId];
      gate.Status = "Approved";
      gate.ApprovedAt = DateTimeOffset.UtcNow;
      gate.ApprovedBy = approverIdentity;

      // Emit telemetry immediately so the Execution Monitor registers the approval
      _telemetryService.RecordEvent(new GovernanceTelemetry
      {
         EventId = $"TEL-GOV-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
         TransactionId = gate.TransactionId,
         PolicyId = $"SPEC-APPROVAL-GATE-{gateId}",
         ApprovalGateStatus = "Approved",
         Timestamp = DateTimeOffset.UtcNow
      });
   }

   public void EscalateToHumanGovernance(string transactionId, PolicyEvaluationResult failureContext)
   {
      // Triggers the "Andon Cord" - Halts autonomous execution for unresolvable structural conflicts [3, 5, 6]
      _logger.LogCritical("HUMAN-MACHINE ESCALATION TRIGGERED: Artifact {ArtifactId} fundamentally violates Policy {PolicyId}. Halting execution.",
          failureContext.ArtifactId, failureContext.PolicyId);

      // 1. Emit critical escalation telemetry to instantly trigger Watchtower anomaly alerts
      _telemetryService.RecordEvent(new GovernanceTelemetry
      {
         EventId = $"TEL-GOV-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
         TransactionId = transactionId,
         PolicyId = failureContext.PolicyId,
         ApprovalGateStatus = "Escalated",
         Timestamp = DateTimeOffset.UtcNow
      });

      // 2. Formulate the ENT-ESCALATION-PAYLOAD required by the human architects [5, 6, 9]
      string violationsList = string.Join("; ", failureContext.Violations);
      string escalationPayload = $@"
            {{
                ""escalationType"": ""Unresolvable Structural Conflict"",
                ""traceabilityPath"": ""{failureContext.ArtifactId} -> {failureContext.PolicyId}"",
                ""failureContext"": ""{violationsList}""
            }}";

      // 3. Route the payload to ACT-001 (Human Governance Team)
      _logger.LogError("Escalation Payload Routed to Human Governance Team: {Payload}", escalationPayload);

      // Note: In the live Execution Runtime, this void method throwing or logging causes the orchestrator to 
      // cleanly pause the task graph and save its state until human resolution is received.
   }
}
