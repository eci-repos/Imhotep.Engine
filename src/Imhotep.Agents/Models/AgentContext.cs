using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Agents.Models;

/// <summary>
/// ISL v2.1 Sec 9.1: Agent Context Package Schema.
/// Defines the explicitly bounded contextual information provided to an agent.
/// </summary>
public record AgentContextPackage
{
   public string ContextPackageId { get; init; } = Guid.NewGuid().ToString();
   public required string AgentRole { get; init; }
   public required string TaskId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   // Core canonical context
   public required IReadOnlyList<ICanonicalEntity> IncludedEntities { get; init; }

   // Optional operational context [ISL v2.1 Sec 9.1]
   public IReadOnlyList<string>? IncludedArtifacts { get; init; }
   public IReadOnlyList<string>? IncludedValidationResults { get; init; }
   public IReadOnlyList<string>? IncludedGovernanceConstraints { get; init; }

   // Added the missing fields from ISL v2.1 Sec 9.1:
   public IReadOnlyList<string>? IncludedRepairRecords { get; init; }
   public IReadOnlyList<string>? IncludedPriorAgentOutputs { get; init; }
   public IReadOnlyList<string>? Exclusions { get; init; } // Documents intentionally withheld context
   public object? ContextSizeMetrics { get; init; } // e.g., token counts

   /// <summary>
   /// e.g., public, internal, confidential, restricted
   /// </summary>
   public required string SensitivityClassification { get; init; }

   public required DateTimeOffset AssembledAt { get; init; }
   public required string AssembledBy { get; init; }
}

/// <summary>
/// ISL v3.4 Sec 8.1: Agent Runtime Request Schema.
/// The formal payload the Orchestrator uses to dispatch work to an Agent Implementation.
/// </summary>
public record AgentRuntimeRequest
{
   public required string AgentRuntimeRequestId { get; init; } = $"ARR-{Guid.NewGuid():N}";
   public required string AgentInvocationId { get; init; } = $"AINV-{Guid.NewGuid():N}";
   public required string AgentImplementationId { get; init; }
   public required string AgentRole { get; init; }
   public required string TaskId { get; init; }
   public required string ContextPackageId { get; init; }
   public required string OutputContractId { get; init; }
   public required string InvocationMode { get; init; } // generate, repair, review, etc.
   public required int TimeoutSeconds { get; init; }
   public required string CorrelationId { get; init; }
   public required DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents the strict structured output returned by an agent (ISL v3.8).
/// </summary>
public class AgentResult
{
   public string? TargetTraceabilityId { get; set; }
   public bool IsSuccess { get; set; }
   public Dictionary<string, string> GeneratedArtifacts { get; set; } = new Dictionary<string, string>();
   public string? StructuredOutput { get; set; }
   public string? ErrorMessage { get; set; }
}

/// <summary>
/// The standard runtime response returned by an agent implementation (ISL v3.4 Section 8.2).
/// </summary>
public record AgentRuntimeResponse
{
   public string AgentRuntimeResponseId { get; init; } = Guid.NewGuid().ToString();
   public required string AgentInvocationId { get; init; }

   /// <summary>
   /// e.g., completed, failed, timeout, cancelled, escalated
   /// </summary>
   public required string Outcome { get; init; }

   /// <summary>
   /// e.g., not-evaluated, passed, failed, warning
   /// </summary>
   public required string ValidationStatus { get; init; }

   public IReadOnlyList<string>? ProducedArtifactCandidates { get; init; }
   public IReadOnlyList<string>? Findings { get; init; }

   /// <summary>
   /// e.g., accept, retry, revise, validate-artifacts, review, repair, escalate, reject
   /// </summary>
   public required string NextAction { get; init; }

   public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// ISL v2.1 Sec 10.1 & ISL v3.4 Sec 13.1: Agent Output Implementation Schema.
/// The rigid, machine-parseable contract returned by the agent.
/// </summary>
public record AgentOutputRecord
{
   public required string AgentOutputId { get; init; } = $"OUT-{Guid.NewGuid():N}";
   public required string AgentInvocationId { get; init; }

   // Required by ISL v3.4 Sec 13.1
   public string? AgentImplementationId { get; init; }

   public required string AgentRole { get; init; }
   public required string TaskId { get; init; }
   public required string OutputType { get; init; }
   public string? Summary { get; init; }

   // Restored from ISL v2.1 Section 10.1
   public object? StructuredOutput { get; init; }
   public required string Confidence { get; init; } // e.g., high, medium, low, unknown
   public required bool RequiresReview { get; init; }

   public IReadOnlyList<string>? ProducedArtifactCandidates { get; init; }
   public required IReadOnlyList<string> ReferencedEntities { get; init; }

   public required bool RequiresDeterministicValidation { get; init; }
   public required string OutputStatus { get; init; } // valid, invalid, escalated
   public required DateTimeOffset ProducedAt { get; init; } = DateTimeOffset.UtcNow;
}

