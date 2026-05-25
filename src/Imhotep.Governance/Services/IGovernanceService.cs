
using System.Threading.Tasks;
using Imhotep.Governance.Models;

namespace Imhotep.Governance.Services;

/// <summary>
/// ISL v1.7: Enforces organizational policies, manages formal human approval gates, 
/// and ensures autonomous construction remains compliant, accountable, and auditable.
/// </summary>
public interface IGovernanceService
{
   /// <summary>
   /// ISL v1.7 Section 14.0: Triggers a Human-Machine Escalation when the platform cannot 
   /// safely proceed autonomously. Halts the affected branch and requests human intervention [4, 8].
   /// </summary>
   Task<GovernanceEscalationRecord> EscalateToHumanGovernanceAsync(
       string transactionId,
       EscalationPayload escalationPayload,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 16.1: Evaluates a formal governance check request dynamically at runtime.
   /// The Execution Runtime MUST obey the resulting GovernanceCheckResponse decision (e.g., allow, block, escalate).
   /// </summary>
   Task<GovernanceCheckResponse> EvaluateGovernanceCheckAsync(GovernanceCheckRequest request, CancellationToken cancellationToken = default);

   /// <summary>
   /// Checks the status of a specific approval gate to determine if autonomous execution is authorized.
   /// </summary>
   Task<ApprovalGateRecord> GetApprovalGateStatusAsync(string gateId, CancellationToken cancellationToken = default);

   /// <summary>
   /// Registers a formal human sign-off on an Approval Gate, securely recording the action.
   /// Must enforce Separation of Duties (ISL v1.7 Sec 7.0).
   /// </summary>
   Task RegisterHumanApprovalAsync(string gateId, string approverIdentity, string role, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 14.0: Triggers a formal Human-Machine Escalation when deterministic tools 
   /// or repair cycles encounter an unresolvable structural conflict.
   /// </summary>
   Task OpenEscalationAsync(GovernanceEscalationRecord escalation, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 19.0: Records an immutable governance or boundary state transition event for audit purposes.
   /// </summary>
   Task RecordAuditEventAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

