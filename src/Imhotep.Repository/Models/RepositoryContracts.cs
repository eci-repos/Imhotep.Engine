
using System;
using System.Collections.Generic;

namespace Imhotep.Repository.Models;

/// <summary>
/// Represents a tangible output of the autonomous construction process 
/// (e.g., a C# source file, a JSON schema, or an infrastructure manifest).
/// </summary>
public record SoftwareArtifact
{
   public required string ArtifactId { get; init; }

   public required string TransactionId { get; init; }

   /// <summary>
   /// The logical path within the structured workspace reflecting architectural boundaries.
   /// </summary>
   public required string FilePath { get; init; }

   public required string Content { get; init; }

   /// <summary>
   /// Categorizes the artifact (e.g., "SourceCode", "Configuration", "Test", "Documentation").
   /// </summary>
   public required string Category { get; init; }

   // Traceability Links tying the artifact back to the blueprint and execution
   public required string SourceTraceabilityId { get; init; }
   public required string GeneratingTaskId { get; init; }
   public required string GeneratingAgentRole { get; init; }
}

/// <summary>
/// Represents a collection of stable artifacts packaged for operational deployment.
/// </summary>
public record DeploymentPackage
{
   public required string PackageId { get; init; }
   public required string TransactionId { get; init; }
   public IReadOnlyList<SoftwareArtifact> Artifacts { get; init; } = new List<SoftwareArtifact>();
   public required string PackagePath { get; init; }
   public required DateTimeOffset PackagedAt { get; init; }
}

