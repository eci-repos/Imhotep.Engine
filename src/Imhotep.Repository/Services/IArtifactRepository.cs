
using System.Threading.Tasks;
using Imhotep.Repository.Models;

namespace Imhotep.Repository.Services;

/// <summary>
/// ISL v2.3: Manages the structured workspace and version-controlled storage for all generated artifacts, 
/// acting as the final resting place for the autonomous SDLC pipeline.
/// </summary>
public interface IArtifactRepository
{
   // --- REQUIRED METADATA QUERY (ISL v2.3 Sec 27.1) ---

   /// <summary>
   /// ISL v2.3 Sec 27.1: Retrieves the structured metadata record for an artifact, 
   /// including its lifecycle state, traceability, and boundary origins.
   /// </summary>
   Task<ArtifactMetadataRecord?> GetArtifactMetadataAsync(string artifactId, CancellationToken cancellationToken = default);

   // --- EXISTING PAYLOAD & LIFECYCLE METHODS ---

   /// <summary>
   /// Saves a newly generated or repaired artifact to the repository and records its traceability links.
   /// </summary>
   Task SaveArtifactAsync(ArtifactMetadataRecord artifact, byte[] payload, CancellationToken cancellationToken = default);

   /// <summary>
   /// Retrieves a specific version of a generated artifact for review or repair analysis.
   /// </summary>
   Task<byte[]?> GetArtifactPayloadAsync(string repositoryLocation, CancellationToken cancellationToken = default);

   /// <summary>
   /// Commits the current stable state of the repository to version control (e.g., Git) 
   /// ensuring a chronological record of how the system has been constructed.
   /// </summary>
   Task CommitChangesAsync(string transactionId, string commitMessage, CancellationToken cancellationToken = default);

   /// <summary>
   /// Consolidates stable artifacts into an operational deployment payload ready for handoff.
   /// </summary>
   Task<PackageManifestRecord> CreateDeploymentPackageAsync(string transactionId, CancellationToken cancellationToken = default);
}
