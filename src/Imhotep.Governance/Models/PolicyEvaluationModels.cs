using System;
using System.Collections.Generic;

namespace Imhotep.Governance.Models
{
   /// <summary>
   /// Represents a formal request for the Governance Engine to evaluate a generated artifact 
   /// against a specific organizational, security, or regulatory policy (ISL v1.7).
   /// </summary>
   public record PolicyEvaluationRequest
   {
      public required string TransactionId { get; init; }

      /// <summary>
      /// The execution task that generated the artifact being evaluated.
      /// </summary>
      public required string TaskId { get; init; }

      /// <summary>
      /// The unique identifier of the artifact to be evaluated.
      /// </summary>
      public required string ArtifactId { get; init; }

      /// <summary>
      /// The specific governance policy constraint to evaluate against (e.g., "POL-CJIS-001").
      /// </summary>
      public required GovernancePolicy TargetPolicy { get; init; }

      /// <summary>
      /// The structured context or file content of the artifact to be analyzed by the deterministic tools.
      /// </summary>
      public required Dictionary<string, string> ArtifactContext { get; init; } = new();
   }

   /// <summary>
   /// Represents the deterministic outcome of a policy evaluation against a generated artifact.
   /// </summary>
   public record PolicyEvaluationResult
   {
      /// <summary>
      /// The unique identifier of the evaluated artifact.
      /// </summary>
      public required string ArtifactId { get; init; }

      /// <summary>
      /// The TraceabilityId of the policy constraint (e.g., "POL-CJIS-001").
      /// </summary>
      public required string PolicyId { get; init; }

      /// <summary>
      /// Indicates whether the artifact perfectly satisfied the policy constraints.
      /// </summary>
      public required bool IsCompliant { get; init; }

      /// <summary>
      /// If IsCompliant is false, this contains the exact, deterministic reasons for the failure. 
      /// These violations are routed directly to the Repair Analyst agent for automated repair.
      /// </summary>
      public IReadOnlyList<string> Violations { get; init; } = new List<string>();
   }
}
