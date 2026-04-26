using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Governance.Models;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;
using Imhotep.Repository.Services; // Assuming the GitArtifactRepository we built
using Imhotep.SemanticModel.Graph; // For the CanonicalSemanticModel reference in telemetry

namespace Imhotep.Governance.Services
{
   public interface IApprovalGateManager
   {
      Task InitializeRequiredGatesAsync(string transactionId);
      Task<ReadinessLevel> EvaluateReadinessTransitionAsync(string transactionId);
   }

   /// <summary>
   /// Enforces formal human checkpoints required before a specification can advance to autonomous execution [13, 14].
   /// </summary>
   public class ApprovalGateManager : IApprovalGateManager
   {
      private readonly IGovernanceService _governanceService;
      private readonly ITelemetryService _telemetryService;
      private readonly IArtifactRepository _artifactRepository;
      private readonly ILogger<ApprovalGateManager> _logger;

      // The strict roster of Human Governance Roles required for enterprise Approval Gates [4-6]
      private readonly string[] _mandatoryGateRoles = new[]
      {
            "IT Architect",
            "Security Validator",
            "Court Auditor/CDO"
        };

      public ApprovalGateManager(
          IGovernanceService governanceService,
          ITelemetryService telemetryService,
          IArtifactRepository artifactRepository,
          ILogger<ApprovalGateManager> logger)
      {
         _governanceService = governanceService;
         _telemetryService = telemetryService;
         _artifactRepository = artifactRepository;
         _logger = logger;
      }

      /// <summary>
      /// Seeds the mandatory Approval Gates when a blueprint enters the Reviewable level.
      /// </summary>
      public async Task InitializeRequiredGatesAsync(string transactionId)
      {
         _logger.LogInformation("Initializing mandatory Approval Gates for Transaction {TransactionId}", transactionId);

         foreach (var role in _mandatoryGateRoles)
         {
            var gateId = $"GATE-{role.Replace(" ", "").Replace("/", "-").ToUpper()}-{transactionId}";

            // In an active system, this creates a Pending gate in the Governance State database
            // awaiting the RegisterHumanApproval call from the specific role.
            _logger.LogDebug("Mandatory Approval Gate created: {GateId} requiring role: {Role}", gateId, role);
         }

         await Task.CompletedTask;
      }

      /// <summary>
      /// Evaluates if all required Human Governance Roles have signed off, authorizing the transition 
      /// to the Machine-Valid and Autonomous-Ready levels [2, 15].
      /// </summary>
      public async Task<ReadinessLevel> EvaluateReadinessTransitionAsync(string transactionId)
      {
         _logger.LogInformation("Evaluating Approval Gates for Transaction {TransactionId} readiness transition.", transactionId);

         bool allGatesCleared = true;

         foreach (var role in _mandatoryGateRoles)
         {
            var gateId = $"GATE-{role.Replace(" ", "").Replace("/", "-").ToUpper()}-{transactionId}";
            var gate = await _governanceService.GetApprovalGateStatusAsync(transactionId, gateId);

            if (gate.Status != "Approved")
            {
               _logger.LogWarning("Readiness Blocked: Approval Gate {GateId} is currently {Status}. Required Role: {Role}",
                   gate.GateId, gate.Status, gate.RequiredRole);
               allGatesCleared = false;
            }
         }

         if (!allGatesCleared)
         {
            _logger.LogInformation("Transaction {TransactionId} remains at the Reviewable Level pending further human governance.", transactionId);
            return ReadinessLevel.Reviewable;
         }

         // 1. All Gates Cleared: Transition to Machine-Valid & Autonomous-Ready [6, 15]
         _logger.LogInformation("All Approval Gates successfully cleared for Transaction {TransactionId}. Authorizing autonomous execution.", transactionId);

         // 2. Commit the Baseline to the Artifact Repository
         // This mechanical handoff establishes the authoritative architectural baseline for traceability [15].
         await _artifactRepository.CommitChangesAsync(
             transactionId,
             "Blueprint transitioned to Autonomous-Ready. Establishing authoritative architectural baseline."
         );

         // 3. Emit Governance Telemetry to the Watchtower Dashboard
         _telemetryService.RecordEvent(new GovernanceTelemetry
         {
            EventId = $"TEL-GOV-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            TransactionId = transactionId,
            PolicyId = "SPEC-READINESS-LIFECYCLE",
            ApprovalGateStatus = "Autonomous-Ready",
            Timestamp = DateTimeOffset.UtcNow
         });

         // The Execution Runtime and Planning Engine can now safely take over
         return ReadinessLevel.AutonomousReady;
      }
   }
}
