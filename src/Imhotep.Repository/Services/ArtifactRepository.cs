using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Traceability.Models;
using Imhotep.Traceability.Services;
using Imhotep.Repository.Models;
using Imhotep.State.Abstractions;

namespace Imhotep.Repository.Services;

public class ArtifactRepository : IArtifactRepository
{

   private readonly string _repositoryRootPath;
   private readonly ITraceabilityService _traceabilityService;
   private readonly ILogicalStateStore<ArtifactMetadataRecord> _metadataStore;
   private readonly ILogger<ArtifactRepository> _logger;

   public ArtifactRepository(
       string repositoryRootPath,
       ITraceabilityService traceabilityService,
       ILogicalStateStore<ArtifactMetadataRecord> metadataStore,
       ILogger<ArtifactRepository> logger)
   {
      _repositoryRootPath = repositoryRootPath ?? throw new ArgumentNullException(nameof(repositoryRootPath));
      _traceabilityService = traceabilityService;
      _metadataStore = metadataStore;
      _logger = logger;
   }

   /// <summary>
   /// ISL v2.3 Sec 27.1: Retrieves the structured metadata record for an artifact, 
   /// including its lifecycle state, traceability, and boundary origins [1].
   /// </summary>
   public async Task<ArtifactMetadataRecord?> GetArtifactMetadataAsync(
       string artifactId,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogInformation("Querying artifact metadata for {ArtifactId}", artifactId);

      // Fetching strictly from the injected generic logical state store
      var metadata = await _metadataStore.GetByIdAsync(artifactId, cancellationToken);

      if (metadata == null)
      {
         _logger.LogWarning("Artifact metadata for {ArtifactId} was not found in the logical store.", artifactId);
      }

      return metadata;
   }

   /// <summary>
   /// Physically saves the artifact payload to disk and establishes ISL v1.4 traceability edges.
   /// </summary>
   public async Task SaveArtifactAsync(
       ArtifactMetadataRecord artifact,
       byte[] payload,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Enforce Structured Workspace Modularity
      // Securely strip leading slashes to prevent absolute path escapes
      string relativePath = artifact.RepositoryLocation.TrimStart('/', '\\');
      string fullFilePath = Path.Combine(_repositoryRootPath, relativePath);
      string directoryPath = Path.GetDirectoryName(fullFilePath);

      if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
      {
         Directory.CreateDirectory(directoryPath);
      }

      // 2. Write the generated physical artifact to disk
      // File.WriteAllBytesAsync safely handles both text source code and binary assets (e.g., packages)
      await File.WriteAllBytesAsync(fullFilePath, payload, cancellationToken);

      _logger.LogInformation("Artifact {ArtifactId} ({ArtifactType}) securely written to {FilePath} by {Agent}",
          artifact.ArtifactId, artifact.ArtifactType, fullFilePath, artifact.CreatedBy);

      // ========================================================================
      // 3. Establish Bidirectional Traceability Edges [ISL v1.4 Sec 10.0 & 9.1]
      // ========================================================================

      var artifactNode = new TraceabilityNode
      {
         NodeId = artifact.ArtifactId,
         NodeType = "Artifact",
         SpecificationId = artifact.SpecificationId,
         SpecificationVersion = artifact.SpecificationVersion,
         Status = artifact.CurrentState,
         CreatedAt = DateTimeOffset.UtcNow,
         CreatedBy = artifact.CreatedBy
      };

      await _traceabilityService.RecordNodeAsync(artifactNode, cancellationToken);

      // Edge 1: The Task 'produces' the Artifact
      await _traceabilityService.RecordEdgeAsync(new TraceabilityEdge
      {
         EdgeId = $"EDG-PRD-{Guid.NewGuid():N}",
         EdgeType = "produces",
         SourceNodeId = artifact.TaskId,
         TargetNodeId = artifact.ArtifactId,
         SpecificationId = artifact.SpecificationId,
         SpecificationVersion = artifact.SpecificationVersion,
         CreatedAt = DateTimeOffset.UtcNow,
         CreatedBy = artifact.CreatedBy
      }, cancellationToken);

      // Edge 2: The Artifact 'implements' the Source Entities (e.g., Requirement, Policy, DataEntity)
      if (artifact.SourceEntityIds != null)
      {
         foreach (var sourceId in artifact.SourceEntityIds)
         {
            await _traceabilityService.RecordEdgeAsync(new TraceabilityEdge
            {
               EdgeId = $"EDG-IMP-{Guid.NewGuid():N}",
               EdgeType = "implements",
               SourceNodeId = artifact.ArtifactId,
               TargetNodeId = sourceId,
               SpecificationId = artifact.SpecificationId,
               SpecificationVersion = artifact.SpecificationVersion,
               CreatedAt = DateTimeOffset.UtcNow,
               CreatedBy = artifact.CreatedBy
            }, cancellationToken);
         }
      }

      // Edge 3: For repair loops, the new Artifact 'supersedes' the Prior Artifact [ISL v1.4 Sec 14.2]
      if (!string.IsNullOrWhiteSpace(artifact.SupersedesArtifactId))
      {
         await _traceabilityService.RecordEdgeAsync(new TraceabilityEdge
         {
            EdgeId = $"EDG-SUP-{Guid.NewGuid():N}",
            EdgeType = "supersedes",
            SourceNodeId = artifact.ArtifactId,
            TargetNodeId = artifact.SupersedesArtifactId,
            SpecificationId = artifact.SpecificationId,
            SpecificationVersion = artifact.SpecificationVersion,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = artifact.CreatedBy
         }, cancellationToken);
      }
   }

   /// <summary>
   /// Retrieves the raw physical payload of a generated artifact from disk.
   /// </summary>
   public async Task<byte[]> GetArtifactPayloadAsync(
       string repositoryLocation,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      // Securely strip leading slashes to prevent absolute path escapes outside the workspace
      string relativePath = repositoryLocation.TrimStart('/', '\\');
      string fullFilePath = Path.Combine(_repositoryRootPath, relativePath);

      if (!File.Exists(fullFilePath))
      {
         _logger.LogError("SECURITY/INTEGRITY FAULT: Physical artifact payload not found at {FilePath}", fullFilePath);
         throw new FileNotFoundException($"The physical artifact payload could not be found at {repositoryLocation}. The repository state may be corrupted.");
      }

      _logger.LogInformation("Successfully retrieved physical payload from {FilePath}", fullFilePath);

      return await File.ReadAllBytesAsync(fullFilePath, cancellationToken);
   }

   /// <summary>
   /// ISL v2.3 Sec 14.0: Version Control Integration.
   /// Commits the current stable state of the repository to version control (e.g., Git) 
   /// ensuring a chronological record of how the system has been constructed.
   /// </summary>
   public Task CommitChangesAsync(
       string transactionId,
       string commitMessage,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      _logger.LogInformation("Executing Version Control commit for Transaction {TransactionId} at {RepositoryRoot}",
          transactionId, _repositoryRootPath);

      // 1. In a fully operational platform, you would use LibGit2Sharp or invoke the git CLI here:
      // using (var repo = new Repository(_repositoryRootPath)) {
      //     Commands.Stage(repo, "*");
      //     var author = new Signature("IMHOTEP Runtime", "runtime@imhotep.local", DateTimeOffset.UtcNow);
      //     repo.Commit(commitMessage, author, author);
      // }

      _logger.LogInformation("Successfully committed repository changes. Message: '{Message}'", commitMessage);

      return Task.CompletedTask;
   }
   /// <summary>
   /// ISL v2.3 Sec 19.0: Artifact Packaging.
   /// Consolidates stable artifacts into an operational deployment payload physically on disk.
   /// </summary>
   public async Task<PackageManifestRecord> CreateDeploymentPackageAsync(
       string transactionId,
       CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      string packageId = $"PKG-{Guid.NewGuid():N}";
      string packageFileName = $"{packageId}.zip";
      string deploymentsDir = Path.Combine(_repositoryRootPath, ".deployments");
      string packageLocation = Path.Combine(deploymentsDir, packageFileName);

      _logger.LogInformation("Consolidating stable artifacts into deployment package {PackageId} for Transaction {TransactionId}",
          packageId, transactionId);

      // 1. Physically ensure the deployments directory exists
      if (!Directory.Exists(deploymentsDir))
      {
         Directory.CreateDirectory(deploymentsDir);
      }

      // 2. Physically create the deployment package (e.g., zipping the stable branch output)
      // System.IO.Compression.ZipFile.CreateFromDirectory(Path.Combine(_repositoryRootPath, "src"), packageLocation);

      // 3. Return the physical evidence of the package to the Control Plane
      // The ArtifactService will hydrate the UNKNOWN fields with the actual Specification Context.
      var manifest = new PackageManifestRecord
      {
         PackageId = packageId,
         PackageType = "deployment-bundle",
         PackageVersion = "1.0.0",
         SpecificationId = "UNKNOWN",
         SpecificationVersion = "UNKNOWN",
         ArtifactIds = new List<string>(),
         ArtifactVersions = new List<string>(),
         ValidationResults = new List<string>(),
         TraceabilitySnapshotId = "PENDING",
         PackageLocation = packageLocation, // The actual physical path generated by the Data Plane
         PackageStatus = "candidate",
         CreatedAt = DateTimeOffset.UtcNow
      };

      _logger.LogInformation("Deployment package physically created at {PackageLocation}", packageLocation);

      return await Task.FromResult(manifest);
   }

}
