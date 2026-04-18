
using System.Threading.Tasks;
using Imhotep.Governance.Models;

namespace Imhotep.Governance.Services;

/// <summary>
/// Enforces organizational policies and manages formal human approval gates to ensure 
/// the autonomous construction process remains compliant and accountable.
/// </summary>
public interface IGovernanceService
{
   /// <summary>
   /// Evaluates generated artifacts or execution states against defined governance policies 
   /// continuously during the execution workflow.
   /// </summary>
   Task<PolicyEvaluationResult> EvaluateComplianceAsync(string artifactId, GovernancePolicy policy);

   /// <summary>
   /// Checks the status of a specific approval gate to determine if autonomous execution is authorized.
   /// </summary>
   Task<ApprovalGate> GetApprovalGateStatusAsync(string transactionId, string gateId);

   /// <summary>
   /// Registers a formal human sign-off on an Approval Gate, securely recording the action 
   /// to unlock the next phase of autonomy.
   /// </summary>
   void RegisterHumanApproval(string gateId, string approverIdentity, string role);

   /// <summary>
   /// Triggers a formal Human-Machine Escalation when deterministic tools or repair cycles 
   /// encounter an unresolvable structural conflict against a Mandatory policy.
   /// </summary>
   void EscalateToHumanGovernance(string transactionId, PolicyEvaluationResult failureContext);
}

