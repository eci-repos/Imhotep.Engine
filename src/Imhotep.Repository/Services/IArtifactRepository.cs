
using System.Threading.Tasks;
using Imhotep.Repository.Models;

namespace Imhotep.Repository.Services;

/// <summary>
/// Manages the structured workspace and version-controlled storage for all generated artifacts, 
/// acting as the final resting place for the autonomous SDLC pipeline.
/// </summary>
public interface IArtifactRepository
{
   /// <summary>
   /// Saves a newly generated or repaired artifact to the repository and records its traceability links.
   /// </summary>
   Task SaveArtifactAsync(SoftwareArtifact artifact);

   /// <summary>
   /// Retrieves a specific version of a generated artifact for review or repair analysis.
   /// </summary>
   Task<SoftwareArtifact?> GetArtifactAsync(string artifactId, string version);

   /// <summary>
   /// Commits the current stable state of the repository to version control (e.g., Git) 
   /// ensuring a chronological record of how the system has been constructed.
   /// </summary>
   Task CommitChangesAsync(string transactionId, string commitMessage);

   /// <summary>
   /// Consolidates stable artifacts into an operational deployment payload ready for handoff.
   /// </summary>
   Task<DeploymentPackage> CreateDeploymentPackageAsync(string transactionId);
}

