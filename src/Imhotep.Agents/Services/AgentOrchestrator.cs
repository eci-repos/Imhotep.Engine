using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Services;
using Imhotep.State.Abstractions;
using Imhotep.State.Models;
using Imhotep.Tools.Gateway;
using Microsoft.Extensions.Logging;

namespace Imhotep.Orchestration.Services;

/// <summary>
/// ISL v2.0 & v3.0: Core Agent Orchestrator Subsystem.
/// Coordinates reasoning agents and assembles strictly bounded context packages.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
   // 1. ISL v2.2 Logical State Stores
   private readonly ILogicalStateStore<ICanonicalEntity> _semanticStore; // NEW: For fetching blueprint data
   private readonly ILogicalStateStore<ToolInvocationResult> _validationStore;
   private readonly ILogicalStateStore<RepairRecord> _repairStore;
   private readonly ILogger<AgentOrchestrator> _logger;

   // 2. Inject dependencies via constructor
   public AgentOrchestrator(
       ILogicalStateStore<ICanonicalEntity> semanticStore,
       ILogicalStateStore<ToolInvocationResult> validationStore,
       ILogicalStateStore<RepairRecord> repairStore,
       ILogger<AgentOrchestrator> logger)
   {
      _semanticStore = semanticStore;
      _validationStore = validationStore;
      _repairStore = repairStore;
      _logger = logger;
   }

   /// <summary>
   /// ISL v2.1 Sec 9.2: Context Inclusion Rules.
   /// Assembles the explicitly bounded context package for a specific agent role and task.
   /// </summary>
   public async Task<AgentContextPackage> AssembleContextAsync(
       ConstructionTask task,
       string agentRole,
       CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Assembling ISL v2.1 Context Package for Task {TaskId} and Role {AgentRole}", task.TaskId, agentRole);

      // 3. Hydrate Canonical Entities [ISL v2.1 Sec 9.2]
      // We iterate over the Traceability Identifiers and fetch the actual domain objects so the AI can reason over them.
      var canonicalEntities = new List<ICanonicalEntity>();
      if (task.SourceEntityIds != null)
      {
         foreach (var entityId in task.SourceEntityIds)
         {
            var entity = await _semanticStore.GetByIdAsync(entityId, cancellationToken);
            if (entity != null)
            {
               canonicalEntities.Add(entity);
            }
            else
            {
               _logger.LogWarning("Source Entity {EntityId} missing from Semantic Store. Task {TaskId} may lack context.", entityId, task.TaskId);
            }
         }
      }

      var validationResultIds = new List<string>();
      var repairRecordIds = new List<string>();
      var governanceConstraints = task.GovernanceConstraints?.ToList() ?? new List<string>();

      // 4. Role-Specific Context Inclusion [ISL v2.1 Sec 9.2]
      if (agentRole.Equals("Repair Analyst", StringComparison.OrdinalIgnoreCase))
      {
         var failures = await _validationStore.FindAsync(
             v => v.TaskId == task.TaskId && !v.Outcome.Equals("passed", StringComparison.OrdinalIgnoreCase),
             cancellationToken);

         validationResultIds.AddRange(failures.Select(f => f.ToolInvocationResultId));

         var repairs = await _repairStore.FindAsync(r => r.TaskId == task.TaskId, cancellationToken);
         repairRecordIds.AddRange(repairs.Select(r => r.RepairId));
      }
      else if (agentRole.Equals("Security Validator", StringComparison.OrdinalIgnoreCase))
      {
         governanceConstraints.Add("POL-SECURITY-BASE");
      }

      // 5. Assemble the formal Agent Context Package
      return new AgentContextPackage
      {
         ContextPackageId = $"CTX-PKG-{Guid.NewGuid():N}",
         AgentRole = agentRole,
         TaskId = task.TaskId,

         SpecificationId = "SPEC-CURRENT",
         SpecificationVersion = "1.0.0",

         // Securely attach the read-only collection of full ICanonicalEntity objects
         IncludedEntities = canonicalEntities.AsReadOnly(),

         IncludedArtifacts = task.ArtifactsConsumed?.ToList().AsReadOnly(),
         IncludedValidationResults = validationResultIds.Any() ? validationResultIds.AsReadOnly() : null,
         IncludedRepairRecords = repairRecordIds.Any() ? repairRecordIds.AsReadOnly() : null,
         IncludedGovernanceConstraints = governanceConstraints.Any() ? governanceConstraints.AsReadOnly() : null,

         SensitivityClassification = "internal",

         AssembledAt = DateTimeOffset.UtcNow,
         AssembledBy = "AgentOrchestrator"
      };
   }

   /// <summary>
   /// ISL v2.1 Sec 8.0 & ISL v3.4 Sec 8.0: Agent Runtime Interface.
   /// Dispatches the bounded context to the specific agent implementation and guarantees a strictly validated output.
   /// </summary>
   public async Task<AgentOutputRecord> InvokeAgentAsync(
       ConstructionTask task,
       AgentContextPackage contextPackage,
       CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Invoking Agent Role '{AgentRole}' for Task {TaskId}", contextPackage.AgentRole, task.TaskId);

      // Dynamic output contract logic...
      var outputContract = task.TaskType switch
      {
         TaskCategory.Schema or TaskCategory.Implementation => "artifact-candidate",
         TaskCategory.Test => "test-plan",
         TaskCategory.Verification => "review-finding",
         TaskCategory.Infrastructure => "deployment-preparation",
         _ => "plan-fragment"
      };

      // 1. ISL v3.4 Sec 8.1: Construct the formal Agent Runtime Request
      var agentRequest = new AgentRuntimeRequest
      {
         AgentRuntimeRequestId = $"ARR-{Guid.NewGuid():N}",
         AgentInvocationId = $"INVOC-{Guid.NewGuid():N}",
         AgentImplementationId = "IMPL-DEFAULT-001", // Resolved via Agent Registry in production
         AgentRole = contextPackage.AgentRole,
         TaskId = task.TaskId,
         ContextPackageId = contextPackage.ContextPackageId,
         OutputContractId = outputContract,
         InvocationMode = task.Status == PlanStatus.InRepair ? "repair" : "generate",
         TimeoutSeconds = 300,
         CorrelationId = $"COR-{Guid.NewGuid():N}",
         RequestedAt = DateTimeOffset.UtcNow
      };

      // 2. ISL v2.5 & ISL v3.8: Route to the specific Agent Implementation.
      // The Agent Implementation will internally use the ModelGateway to securely talk to Semantic Kernel/LLM.
      AgentOutputRecord rawAgentOutput = await DispatchToAgentImplementationAsync(agentRequest, contextPackage, cancellationToken);

      // 3. ISL v3.4 Sec 14.0: Output Validation Enforcement
      // The Orchestrator MUST validate the output against the expected role contract before downstream use.
      var validationResult = ValidateAgentOutput(rawAgentOutput, agentRequest.OutputContractId);

      if (!validationResult.IsValid)
      {
         _logger.LogWarning("Agent {AgentRole} produced invalid output: {Reason}. Triggering Escalation.",
             contextPackage.AgentRole, validationResult.Reason);

         // ISL v3.4 Sec 26.0: Agent Output Schema Invalid -> Retry, Reject, or Escalate
         // For the MACS POC, we safely escalate to avoid downstream crashes
         return rawAgentOutput with
         {
            OutputStatus = "escalated",
            Summary = $"Output Validation Failed: {validationResult.Reason}"
         };
      }

      _logger.LogInformation("Agent {AgentRole} produced valid output. Deterministic Validation Required: {Required}",
          contextPackage.AgentRole, rawAgentOutput.RequiresDeterministicValidation);

      return rawAgentOutput;
   }

   /// <summary>
   /// Simulates dispatching the request to the actual concrete agent (e.g., Implementation Generator).
   /// In an enterprise deployment, this invokes the specific agent which queries the ISL v2.5 IModelGateway.
   /// </summary>
   private Task<AgentOutputRecord> DispatchToAgentImplementationAsync(AgentRuntimeRequest request, AgentContextPackage context, CancellationToken cancellationToken)
   {
      // POC Mock: Returns a simulated successful agent response
      return Task.FromResult(new AgentOutputRecord
      {
         AgentOutputId = $"AO-{Guid.NewGuid():N}",
         AgentInvocationId = request.AgentInvocationId,
         AgentImplementationId = request.AgentImplementationId, // Explicitly map from the request
         AgentRole = request.AgentRole,
         TaskId = request.TaskId,
         OutputType = request.InvocationMode == "repair" ? "repair-proposal" : "artifact-content",

         // REQUIRED BY ISL v2.1 Sec 10.1
         Confidence = "high",
         RequiresReview = true, // Indicates to the Orchestrator that the Review Agent should evaluate this
         StructuredOutput = null, // Optional

         ProducedArtifactCandidates = new List<string> { $"ART-CANDIDATE-{Guid.NewGuid():N}" }.AsReadOnly(),

         // Assuming your ICanonicalEntity interface uses 'Id'. Change back to TraceabilityId if explicitly defined that way.
         ReferencedEntities = context.IncludedEntities.Select(e => e.TraceabilityId).ToList().AsReadOnly(),

         Summary = $"Autonomous reasoning completed for {request.TaskId}",
         RequiresDeterministicValidation = true, // ISL v3.4 Sec 15.2: MUST be true for executable artifacts
         OutputStatus = "valid",
         ProducedAt = DateTimeOffset.UtcNow
      });
   }

   /// <summary>
   /// ISL v3.4 Sec 14.1: Internal validator to guarantee the AI did not hallucinate past its architectural boundaries.
   /// </summary>
   private (bool IsValid, string Reason) ValidateAgentOutput(AgentOutputRecord output, string contractId)
   {
      // 1. Enforce Artifact Production Rules [ISL v3.4 Sec 14.1]
      if (output.ProducedArtifactCandidates == null || !output.ProducedArtifactCandidates.Any())
      {
         if (output.AgentRole.Equals("Implementation Generator", StringComparison.OrdinalIgnoreCase))
         {
            return (false, "Implementation Generator failed to produce any artifact candidates.");
         }
      }

      // 2. Enforce Deterministic Supremacy [ISL v3.4 Sec 15.2]
      if (!output.RequiresDeterministicValidation && output.OutputType.Equals("artifact-content", StringComparison.OrdinalIgnoreCase))
      {
         return (false, "Executable or structural artifacts MUST require deterministic validation.");
      }

      return (true, "Passed");
   }

}
