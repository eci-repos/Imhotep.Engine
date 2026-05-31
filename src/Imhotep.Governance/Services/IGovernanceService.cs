
using System.Threading.Tasks;
using Imhotep.Governance.Models;

namespace Imhotep.Governance.Services;

/// <summary>
/// ISL v1.7: Enforces organizational policies, manages formal human approval gates, 
/// and ensures autonomous construction remains compliant, accountable, and auditable.
/// </summary>
public interface IGovernanceService
{
   // --- 1. Runtime Control & Policy Evaluation ---

   /// <summary>
   /// ISL v1.7 Section 16.0: Evaluates a formal governance check request dynamically at runtime.
   /// The Execution Runtime MUST obey the resulting GovernanceCheckResponse decision (e.g., allow, block, escalate, waiver-required).
   /// </summary>
   Task<GovernanceCheckResponse> EvaluateGovernanceCheckAsync(GovernanceCheckRequest request, CancellationToken cancellationToken = default);


   // --- 2. Approval Gates & Separation of Duties ---

   /// <summary>
   /// ISL v1.7 Section 9.0: Checks the status of a specific approval gate to determine if progression is authorized.
   /// </summary>
   Task<ApprovalGateRecord> GetApprovalGateStatusAsync(string gateId, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 9.0: Registers a formal human sign-off on an Approval Gate, securely recording the action.
   /// </summary>
   Task RegisterHumanApprovalAsync(string gateId, string approverIdentity, string role, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 7.0: Validates that a proposed governance action does not violate 
   /// mandatory Separation of Duties rules for the specified system and risk tier.
   /// </summary>
   Task<bool> ValidateSeparationOfDutiesAsync(string specificationId, string specificationVersion, string proposedIdentity, string proposedRole, CancellationToken cancellationToken = default);


   // --- 3. The Escalation Model ---

   /// <summary>
   /// ISL v1.7 Section 14.0: Opens a formal Human-Machine Escalation when the platform cannot safely proceed.
   /// </summary>
   Task<GovernanceEscalationRecord> OpenEscalationAsync(EscalationPayload payload, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 14.0: Formally resolves an open escalation, returning the authorized next-action 
   /// (e.g., resume, replan, halt) to the Execution Runtime.
   /// </summary>
   Task ResolveEscalationAsync(string escalationId, string resolutionRationale, string nextAction, string resolverIdentity, CancellationToken cancellationToken = default);


   // --- 4. Enterprise Exception Pathways (Waivers & Overrides) ---

   /// <summary>
   /// ISL v1.7 Section 12.0: Grants a time-bound Waiver for a known policy or validation failure, 
   /// requiring compensating controls and risk-tier appropriate authorization.
   /// </summary>
   Task<WaiverRecord> GrantWaiverAsync(WaiverRequest request, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.7 Section 13.0: Applies a privileged Override to bypass a failed automated check or blocked state.
   /// Requires strict authorization from a designated Override Authority.
   /// </summary>
   Task<OverrideRecord> ApplyOverrideAsync(OverrideRequest request, CancellationToken cancellationToken = default);


   // --- 5. Deployment Authorization ---

   /// <summary>
   /// ISL v1.7 Section 17.0: Authorizes the release of a packaged candidate to a specific deployment target.
   /// </summary>
   Task<DeploymentAuthorizationRecord> AuthorizeDeploymentAsync(DeploymentAuthorizationRequest request, CancellationToken cancellationToken = default);


   // --- 6. Immutable Audit Logging ---

   /// <summary>
   /// ISL v1.7 Section 19.0: Records an immutable governance event for audit purposes.
   /// Failure to write this audit log MUST halt the governed action.
   /// </summary>
   Task RecordAuditEventAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
