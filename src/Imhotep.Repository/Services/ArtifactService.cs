using Imhotep.Repository.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Repository.Services;

public interface IArtifactService
{
   /// <summary>
   /// ISL v2.3 Sec 11.0: Artifact Admission.
   /// Accepts a candidate artifact into the repository working state.
   /// Artifacts with missing metadata or untraced sources will be rejected.
   /// </summary>
   Task<ArtifactAdmissionRecord> AdmitArtifactAsync(
         string taskId,
         string artifactName,
         string artifactType,
         string producedByBoundaryId,
         byte[] payload,
         string derivationType,
         IReadOnlyList<string> sourceEntityIds,
         string specificationId,
         string specificationVersion,
         CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v2.3 Sec 18.0: Artifact Validation Evidence.
   /// Binds the objective deterministic validation status and raw evidence 
   /// (from tools/tests) to the generated artifact version.
   /// </summary>
   Task<ArtifactEvidenceRecord> RecordArtifactEvidenceAsync(
         string artifactId,
         string validationResultId,
         string evidenceType,
         string outcome,
         string evidenceLocation,
         CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v2.3 Sec 12.0: Artifact Promotion.
   /// Promotes an artifact to 'stable' state after deterministic validation 
   /// and traceability checks mathematically pass.
   /// </summary>
   Task<ArtifactPromotionRecord> PromoteToStableAsync(
         string artifactId,
         string traceabilitySnapshotId,
         CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v2.3 Sec 13.0: Artifact Versioning and Supersession.
   /// Replaces a failed artifact with a repaired candidate, preserving the chronological 
   /// traceability graph via the 'supersedes' and 'superseded-by' relationships.
   /// </summary>
   Task<ArtifactMetadataRecord> SupersedeArtifactAsync(
         string priorArtifactId,
         string repairTaskId,
         byte[] repairedPayload,
         CancellationToken cancellationToken = default);
}

public class ArtifactService : IArtifactService
{
   private readonly ILogger<ArtifactService> _logger;

   // Simulating the durable artifact metadata repository for the MACS POC
   private readonly ConcurrentDictionary<string, ArtifactMetadataRecord> _artifactStore = new();

   public ArtifactService(ILogger<ArtifactService> logger)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
   }
   /// <summary>
   /// ISL v2.3 Sec 11.0: Artifact Admission.
   /// Accepts a candidate artifact into the repository working state.
   /// </summary>
   public async Task<ArtifactAdmissionRecord> AdmitArtifactAsync(
       string taskId,
       string artifactName,
       string artifactType,
       string producedByBoundaryId,
       byte[] payload,
       string derivationType,
       IReadOnlyList<string> sourceEntityIds,
       string specificationId,
       string specificationVersion,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. ISL v2.3 Sec 11.4: Admission Rules. Artifacts with missing metadata or no source reference MUST be rejected [1].
      if (string.IsNullOrWhiteSpace(taskId))
         throw new ArgumentException("Artifacts MUST be associated with a producing task.");
      if (sourceEntityIds == null || sourceEntityIds.Count == 0)
         throw new ArgumentException("Artifacts MUST have at least one source entity reference [ISL v2.3 Sec 11.4].");

      // 2. Physically save the artifact payload to generate the repository location
      // (Replace this with your actual physical storage logic, e.g., writing to disk, S3, or Blob storage)
      string repositoryLocation = $"/artifacts/{specificationId}/{taskId}/{artifactName}";
      // await _storageProvider.WriteAsync(repositoryLocation, payload, cancellationToken);

      // 3. Create the ISL v2.3 compliant ArtifactMetadataRecord
      var artifactId = $"ART-{Guid.NewGuid():N}";
      var artifactMetadata = new ArtifactMetadataRecord
      {
         ArtifactId = artifactId,
         ArtifactName = artifactName,
         ArtifactType = artifactType,
         ArtifactVersion = "1.0.0",
         TaskId = taskId,
         ProducedByBoundaryId = producedByBoundaryId, // Crucial for the Zero-Trust cross-boundary check
         SourceEntityIds = sourceEntityIds,
         DerivationType = derivationType,
         SpecificationId = specificationId,
         SpecificationVersion = specificationVersion,
         RepositoryLocation = repositoryLocation,
         ContentHash = ComputeHash(payload), // Optional: good practice for integrity

         // Initial Lifecycle States
         CurrentState = "pending", // Transition from candidate -> pending upon successful admission [ISL v2.3 Sec 10.2]
         ValidationStatus = "not-evaluated", // Awaits deterministic tool execution
         TraceabilityStatus = "incomplete", // Awaits the TraceabilityEngine's explicit edges
         GovernanceStatus = "pending",

         CreatedAt = DateTimeOffset.UtcNow,
         UpdatedAt = DateTimeOffset.UtcNow,
         CreatedBy = "ArtifactService"
      };

      // 4. Save metadata to your state store
      _artifactStore.TryAdd(artifactMetadata.ArtifactId, artifactMetadata);

      _logger.LogInformation("Artifact {ArtifactId} admitted to repository in 'pending' state. Location: {Location}",
          artifactMetadata.ArtifactId, artifactMetadata.RepositoryLocation);

      // 5. ISL v2.3 Sec 11.2: Generate and return the formal Artifact Admission Record
      var admissionRecord = new ArtifactAdmissionRecord
      {
         AdmissionId = $"ADM-{Guid.NewGuid():N}",
         ArtifactId = artifactId,
         AdmissionSource = "agent", // The generator of the candidate
         SourceReference = taskId,
         TaskId = taskId,
         RepositoryLocation = repositoryLocation,
         AdmissionOutcome = "admitted",
         AdmittedAt = DateTimeOffset.UtcNow,
         AdmittedBy = "ArtifactService"
      };

      return admissionRecord; // Note: No longer returning Task.FromResult since the method is now async
   }

   private string ComputeHash(byte[] payload)
   {
      using var sha256 = System.Security.Cryptography.SHA256.Create();
      return Convert.ToBase64String(sha256.ComputeHash(payload));
   }

   /// <summary>
   /// ISL v2.3 Sec 18.0: Artifact Validation Evidence.
   /// Binds the objective deterministic validation status and raw evidence 
   /// (from tools/tests) to the generated artifact version and updates its lifecycle state.
   /// </summary>
   public Task<ArtifactEvidenceRecord> RecordArtifactEvidenceAsync(
       string artifactId,
       string validationResultId,
       string evidenceType,
       string outcome,
       string evidenceLocation,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Retrieve the artifact metadata to bind evidence to its specific version
      if (!_artifactStore.TryGetValue(artifactId, out var artifactMetadata))
      {
         _logger.LogError("Attempted to record evidence for unknown Artifact {ArtifactId}", artifactId);
         throw new KeyNotFoundException($"Artifact {artifactId} not found in the repository working state.");
      }

      // 2. Create the ISL v2.3 compliant Artifact Evidence Record [ISL v2.3 Sec 18.2]
      var evidenceRecord = new ArtifactEvidenceRecord
      {
         EvidenceRecordId = $"EVD-{Guid.NewGuid():N}",
         ArtifactId = artifactId,
         ArtifactVersion = artifactMetadata.ArtifactVersion, // Binds strictly to the evaluated version
         EvidenceType = evidenceType,
         ValidationResultId = validationResultId,
         EvidenceLocation = evidenceLocation,
         Outcome = outcome,
         CreatedAt = DateTimeOffset.UtcNow
      };

      // 3. Evaluate State Transition Rules [ISL v2.4 Sec 14.5]
      // "An artifact MAY be marked valid only after required deterministic validation passes."
      // "An artifact MUST be marked failed when validation produces a failed outcome."
      string updatedState = artifactMetadata.CurrentState;

      if (outcome.Equals("passed", StringComparison.OrdinalIgnoreCase))
      {
         updatedState = "valid";
      }
      else if (outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
               outcome.Equals("error", StringComparison.OrdinalIgnoreCase))
      {
         updatedState = "failed";
      }

      // 4. Update the Artifact's Lifecycle State immutably
      var updatedArtifact = artifactMetadata with
      {
         ValidationStatus = outcome,
         CurrentState = updatedState,
         UpdatedAt = DateTimeOffset.UtcNow
      };

      // Save the updated metadata back to the repository state store
      _artifactStore[artifactId] = updatedArtifact;

      // Note: In a full database implementation, you would also save the `evidenceRecord` 
      // to an _evidenceStore repository table here.

      _logger.LogInformation(
          "Evidence {EvidenceId} recorded for Artifact {ArtifactId}. Outcome: {Outcome}. Artifact state transitioned to: {State}.",
          evidenceRecord.EvidenceRecordId, artifactId, outcome, updatedState);

      return Task.FromResult(evidenceRecord);
   }
   /// <summary>
   /// ISL v2.3 Sec 12.0: Artifact Promotion.
   /// Promotes an artifact to 'stable' state after deterministic validation 
   /// and traceability checks mathematically pass.
   /// </summary>
   public Task<ArtifactPromotionRecord> PromoteToStableAsync(
       string artifactId,
       string traceabilitySnapshotId,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Retrieve the artifact metadata
      if (!_artifactStore.TryGetValue(artifactId, out var artifactMetadata))
      {
         _logger.LogError("Attempted to promote unknown Artifact {ArtifactId}", artifactId);
         throw new KeyNotFoundException($"Artifact {artifactId} not found in the repository working state.");
      }

      // 2. Evaluate Promotion Preconditions [ISL v2.3 Sec 12.1]
      // An artifact MUST NOT be promoted to stable unless validation has passed (or is formally waived)
      if (!artifactMetadata.ValidationStatus.Equals("passed", StringComparison.OrdinalIgnoreCase) &&
          !artifactMetadata.ValidationStatus.Equals("waived", StringComparison.OrdinalIgnoreCase))
      {
         _logger.LogError("Promotion rejected. Artifact {ArtifactId} validation status is {Status}.", artifactId, artifactMetadata.ValidationStatus);
         throw new InvalidOperationException($"Zero-Trust Violation: Cannot promote artifact {artifactId}. Validation status must be 'passed' or 'waived'.");
      }

      // Traceability MUST be complete before an artifact can become stable
      if (!artifactMetadata.TraceabilityStatus.Equals("complete", StringComparison.OrdinalIgnoreCase))
      {
         _logger.LogError("Promotion rejected. Artifact {ArtifactId} lacks complete traceability.", artifactId);
         throw new InvalidOperationException($"Traceability Violation: Cannot promote artifact {artifactId}. Traceability status must be 'complete'.");
      }

      // Governance MUST approve (or waive/not require) the promotion
      if (!artifactMetadata.GovernanceStatus.Equals("approved", StringComparison.OrdinalIgnoreCase) &&
          !artifactMetadata.GovernanceStatus.Equals("waived", StringComparison.OrdinalIgnoreCase) &&
          !artifactMetadata.GovernanceStatus.Equals("not-required", StringComparison.OrdinalIgnoreCase))
      {
         _logger.LogError("Promotion rejected. Artifact {ArtifactId} is blocked by Governance Status: {Status}.", artifactId, artifactMetadata.GovernanceStatus);
         throw new InvalidOperationException($"Governance Violation: Artifact {artifactId} lacks required governance approvals.");
      }

      // 3. Immutably update the artifact's lifecycle state to "stable" [ISL v2.3 Sec 12.3]
      string priorState = artifactMetadata.CurrentState;
      var promotedArtifact = artifactMetadata with
      {
         CurrentState = "stable",
         UpdatedAt = DateTimeOffset.UtcNow
      };

      // Save the updated metadata back to the repository state store
      _artifactStore[artifactId] = promotedArtifact;

      // 4. Generate the formal Artifact Promotion Record [ISL v2.3 Sec 12.2]
      var promotionRecord = new ArtifactPromotionRecord
      {
         PromotionId = $"PRM-{Guid.NewGuid():N}",
         ArtifactId = artifactId,
         FromState = priorState,
         ToState = "stable",
         // In a full implementation, you would query the evidence store to attach specific ValidationResultIds here.
         // For the POC, we simulate the bound validation result that authorized this promotion:
         ValidationResults = new List<string> { $"EVD-PASSED-FOR-{artifactId}" },
         TraceabilitySnapshotId = traceabilitySnapshotId,
         PromotedAt = DateTimeOffset.UtcNow,
         PromotedBy = "ArtifactService"
      };

      // Note: A full database implementation would also persist `promotionRecord` to a promotion history table here.

      _logger.LogInformation("Artifact {ArtifactId} successfully promoted from {FromState} to stable state.", artifactId, priorState);

      return Task.FromResult(promotionRecord);
   }

   /// <summary>
   /// ISL v2.3 Sec 13.0: Artifact Versioning and Supersession.
   /// Replaces a failed artifact with a repaired candidate, preserving the chronological 
   /// traceability graph via the 'supersedes' and 'superseded-by' relationships.
   /// </summary>
   public Task<ArtifactMetadataRecord> SupersedeArtifactAsync(
       string priorArtifactId,
       string repairTaskId,
       byte[] repairedPayload,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Retrieve the prior artifact metadata
      if (!_artifactStore.TryGetValue(priorArtifactId, out var priorArtifact))
      {
         _logger.LogError("Attempted to supersede unknown Artifact {ArtifactId}", priorArtifactId);
         throw new KeyNotFoundException($"Artifact {priorArtifactId} not found in the repository working state.");
      }

      // 2. Increment the Semantic Version [ISL v2.3 Sec 13.1]
      // A repair that changes artifact content MUST create a new artifact version.
      string newVersion = IncrementPatchVersion(priorArtifact.ArtifactVersion);

      // 3. Physically save the repaired artifact payload to generate the new repository location
      string newRepositoryLocation = $"/artifacts/{priorArtifact.SpecificationId}/{repairTaskId}/{priorArtifact.ArtifactName}";
      // await _storageProvider.WriteAsync(newRepositoryLocation, repairedPayload, cancellationToken);

      // 4. Create the new repaired artifact candidate metadata [ISL v1.4 Sec 14.2]
      var newArtifactId = $"ART-{Guid.NewGuid():N}";
      var newArtifact = priorArtifact with
      {
         ArtifactId = newArtifactId,
         ArtifactVersion = newVersion,
         TaskId = repairTaskId,
         DerivationType = "repair", // Marks derivation as repair
         RepositoryLocation = newRepositoryLocation,
         ContentHash = ComputeHash(repairedPayload),

         // Lifecycle state transitions
         CurrentState = "repaired", // Artifact modified through repair, requires revalidation [ISL v2.3 Sec 10.1]
         ValidationStatus = "not-evaluated",
         TraceabilityStatus = "incomplete", // Traceability engine will establish new edges

         // Bidirectional Supersession Links [ISL v2.3 Sec 13.2]
         SupersedesArtifactId = priorArtifactId,
         SupersededByArtifactId = null,

         CreatedAt = DateTimeOffset.UtcNow,
         UpdatedAt = DateTimeOffset.UtcNow
      };

      // 5. Update the prior artifact to establish forward traceability [ISL v2.3 Sec 13.2]
      var updatedPriorArtifact = priorArtifact with
      {
         SupersededByArtifactId = newArtifactId,
         CurrentState = "superseded", // Transition to superseded [ISL v2.3 Sec 10.1]
         UpdatedAt = DateTimeOffset.UtcNow
      };

      // 6. Save both records back to the repository state store
      _artifactStore[priorArtifactId] = updatedPriorArtifact;
      _artifactStore[newArtifactId] = newArtifact;

      _logger.LogInformation(
          "Artifact {PriorArtifactId} superseded by {NewArtifactId} (Version {NewVersion}) due to repair task {TaskId}.",
          priorArtifactId, newArtifactId, newVersion, repairTaskId);

      return Task.FromResult(newArtifact); // Returned so the Runtime can send it back to deterministic validation
   }

   // Helper to deterministically increment semantic versioning
   private string IncrementPatchVersion(string version)
   {
      if (Version.TryParse(version, out var semver))
      {
         return $"{semver.Major}.{semver.Minor}.{Math.Max(semver.Build, 0) + 1}";
      }
      return version + "-repaired"; // Fallback if the versioning schema diverges
   }

}

