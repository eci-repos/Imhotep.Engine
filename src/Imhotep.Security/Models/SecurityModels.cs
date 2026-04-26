using System;
using System.Collections.Generic;

namespace Imhotep.Security.Models
{
   /// <summary>
   /// The controlled security boundaries within the IMHOTEP platform (ISL v3.10).
   /// </summary>
   public enum SecurityDomain
   {
      Reasoning,
      ToolExecution,
      ArtifactRepository,
      ModelInteraction,
      Governance
   }

   /// <summary>
   /// Represents a request to access sensitive credentials, ensuring traceability for all secret access.
   /// </summary>
   public record SecretRetrievalRequest
   {
      public required string SecretKey { get; init; }
      public required string TransactionId { get; init; }

      /// <summary>
      /// The specific Agent Role or Subsystem requesting the secret, enforcing Identity and Access Management (IAM).
      /// </summary>
      public required string RequestingActorId { get; init; }
      public required SecurityDomain TargetDomain { get; init; }
   }

   /// <summary>
   /// Represents a detected vulnerability within the software supply chain or generated artifacts.
   /// </summary>
   public record VulnerabilityFinding
   {
      public required string ComponentId { get; init; }
      public required string Severity { get; init; } // e.g., "Critical", "High", "Medium"
      public required string Description { get; init; }
      public required string RemediationGuidance { get; init; }
   }

   /// <summary>
   /// The deterministic outcome of a security or supply chain scan.
   /// </summary>
   public record SecurityValidationResult
   {
      public required bool IsSecure { get; init; }
      public IReadOnlyList<VulnerabilityFinding> Findings { get; init; } = new List<VulnerabilityFinding>();
      public required string ScannedByPolicyId { get; init; }
   }
}
