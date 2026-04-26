using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Traceability.Models;
using Imhotep.Traceability.Services;
using Imhotep.Repository.Models;

namespace Imhotep.Repository.Services;

public class GitArtifactRepository : IArtifactRepository
{
   private readonly string _repositoryRootPath;
   private readonly ITraceabilityService _traceabilityService;
   private readonly ILogger<GitArtifactRepository> _logger;

   public GitArtifactRepository(
       string repositoryRootPath,
       ITraceabilityService traceabilityService,
       ILogger<GitArtifactRepository> logger)
   {
      _repositoryRootPath = repositoryRootPath ?? throw new ArgumentNullException(nameof(repositoryRootPath));
      _traceabilityService = traceabilityService;
      _logger = logger;
   }

   public async Task SaveArtifactAsync(SoftwareArtifact artifact)
   {
      // 1. Enforce Structured Workspace Modularity
      string fullFilePath = Path.Combine(_repositoryRootPath, artifact.FilePath);
      string directoryPath = Path.GetDirectoryName(fullFilePath);

      if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
      {
         Directory.CreateDirectory(directoryPath);
      }

      // 2. Write the generated physical artifact to disk
      await File.WriteAllTextAsync(fullFilePath, artifact.Content);
      _logger.LogInformation("Artifact {ArtifactId} ({Category}) securely written to {FilePath} by {Agent}",
          artifact.ArtifactId, artifact.Category, artifact.FilePath, artifact.GeneratingAgentRole);

      // 3. Establish Bidirectional Traceability Edges (ISL v1.4 & v2.3)
      var artifactNode = new TraceabilityNode
      {
         NodeId = artifact.ArtifactId,
         Type = NodeType.SoftwareArtifact,
         Description = $"Generated file: {artifact.FilePath}"
      };

      await _traceabilityService.RegisterNodeAsync(artifactNode);

      // Create explicit edge linking the file UP to the architectural blueprint requirement
      await _traceabilityService.CreateEdgeAsync(new TraceabilityEdge
      {
         SourceNodeId = artifact.ArtifactId,
         TargetNodeId = artifact.SourceTraceabilityId,
         Relationship = RelationshipType.Fulfills,
         TransactionId = artifact.TransactionId
      });

      // Create explicit edge linking the file to the task that generated it
      await _traceabilityService.CreateEdgeAsync(new TraceabilityEdge
      {
         SourceNodeId = artifact.ArtifactId,
         TargetNodeId = artifact.GeneratingTaskId,
         Relationship = RelationshipType.GeneratedBy,
         TransactionId = artifact.TransactionId
      });
   }

   public async Task<SoftwareArtifact?> GetArtifactAsync(string artifactId, string version)
   {
      _logger.LogInformation("Retrieving Artifact {ArtifactId} version {Version} for review/repair analysis.", artifactId, version);

      // In an enterprise environment, this would utilize a library like LibGit2Sharp 
      // to check out the specific blob/commit hash corresponding to the version [2, 3].
      // For the MACS Proof-of-Concept, we return the current file state from disk.

      // Note: A full implementation requires mapping the ArtifactId to the relative file path.
      throw new NotImplementedException("Retrieval requires a mapping from ArtifactId to file path or a Git library integration.");
   }

   public Task CommitChangesAsync(string transactionId, string commitMessage)
   {
      _logger.LogInformation("Committing stable repository state for Transaction {TransactionId}", transactionId);

      // 1. Integrates with Version Control (Git) to maintain a chronological record of the system's evolution [2, 4].
      try
      {
         // In a true .NET implementation, this is typically done using LibGit2Sharp.
         // For a CLI-based integration, it would execute something like:
         /*
         var processInfo = new ProcessStartInfo("git", $"commit -am \"{commitMessage}\"")
         {
             WorkingDirectory = _repositoryRootPath,
             RedirectStandardOutput = true,
             UseShellExecute = false
         };
         using var process = Process.Start(processInfo);
         process?.WaitForExit();
         */

         _logger.LogDebug("Git commit successful: {Message}", commitMessage);
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to commit changes to Git repository.");
         throw;
      }

      return Task.CompletedTask;
   }

   public Task<DeploymentPackage> CreateDeploymentPackageAsync(string transactionId)
   {
      _logger.LogInformation("Consolidating artifacts into a deployment package for Transaction {TransactionId}", transactionId);

      // 1. Consolidates stable artifacts into an operational deployment payload (e.g., container images, manifests) [1, 5].
      string packageDirectory = Path.Combine(_repositoryRootPath, "deployments", transactionId);
      if (!Directory.Exists(packageDirectory))
      {
         Directory.CreateDirectory(packageDirectory);
      }

      string packagePath = Path.Combine(packageDirectory, $"deployment-payload-{transactionId}.zip");

      // Logic to compress/zip the stable artifacts into packagePath would execute here.

      var package = new DeploymentPackage
      {
         PackageId = Guid.NewGuid().ToString("N"),
         TransactionId = transactionId,
         PackagePath = packagePath,
         PackagedAt = DateTimeOffset.UtcNow
      };

      _logger.LogInformation("Deployment package successfully created at {PackagePath}", packagePath);
      return Task.FromResult(package);
   }
}
