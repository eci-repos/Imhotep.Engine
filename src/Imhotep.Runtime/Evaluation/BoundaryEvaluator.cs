using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Imhotep.Planning.Models;

namespace Imhotep.Runtime.Evaluation;

/// <summary>
/// Enforces Zero-Trust boundary execution by deterministically evaluating 
/// Entry and Exit criteria before and after a boundary runs [1, 2].
/// </summary>
public interface IBoundaryEvaluator
{
   /// <summary>
   /// Evaluates if all dependency boundaries, contexts, governance approvals, and tools are ready (ISL v1.5 Section 19.5).
   /// </summary>
   Task<BoundaryEvaluationResult> EvaluateEntryCriteriaAsync(ConstructionBoundary boundary, CancellationToken cancellationToken = default);

   /// <summary>
   /// Evaluates if all tasks, verification steps, artifacts, and continuation records are complete (ISL v1.5 Section 19.6).
   /// </summary>
   Task<BoundaryEvaluationResult> EvaluateExitCriteriaAsync(ConstructionBoundary boundary, CancellationToken cancellationToken = default);
}

/// <summary>
/// Actively verifies the ValidationRule and TrustPolicy contracts before authorizing 
/// cross-boundary interactions (ISL v1.5 Section 19.4) [1, 3].
/// </summary>
public interface IConnectionContextValidator
{
   /// <summary>
   /// Evaluates the connection context to authorize a downstream boundary to consume upstream artifacts or contexts.
   /// </summary>
   Task<ConnectionValidationResult> ValidateConnectionAsync(ConnectionContext context, CancellationToken cancellationToken = default);
}

// --- Immutable Result Records ---

public record BoundaryEvaluationResult
{
   public required bool IsAuthorized { get; init; }
   public required IReadOnlyList<string> UnmetCriteria { get; init; }
   public required DateTimeOffset EvaluatedAt { get; init; }
}

public record ConnectionValidationResult
{
   public required bool IsValid { get; init; }
   public required bool TrustPolicySatisfied { get; init; }
   public required IReadOnlyList<string> MissingElements { get; init; }
   public required IReadOnlyList<string> ViolationReasons { get; init; }
   public required DateTimeOffset ValidatedAt { get; init; }
}
