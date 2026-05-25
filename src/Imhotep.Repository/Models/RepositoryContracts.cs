
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

/// <summary>
/// ISL v2.3 Sec 8.1: Artifact Metadata Schema.
/// Represents the authoritative lifecycle state, identity, and traceability of an artifact.
/// </summary>
public record ArtifactMetadataRecord
{
   public required string ArtifactId { get; init; } = $"ART-{Guid.NewGuid():N}";
   public required string ArtifactName { get; init; }
   public required string ArtifactType { get; init; } // source, config, schema, test, etc.
   public required string ArtifactVersion { get; init; }

   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   public required string TaskId { get; init; }
   public required string ProducedByBoundaryId { get; init; } // Supports Zero-Trust cross-boundary checks
   public required IReadOnlyList<string> SourceEntityIds { get; init; }
   public required string DerivationType { get; init; } // direct, derived, inferred, repair
   public required string RepositoryLocation { get; init; }
   public string? ContentHash { get; init; }
   public required string CurrentState { get; init; } // pending, valid, failed, repaired, stable, superseded
   public required string ValidationStatus { get; init; } // not-evaluated, passed, failed, warning, waived
   public required string TraceabilityStatus { get; init; } // complete, incomplete, inconsistent
   public required string GovernanceStatus { get; init; } // not-required, pending, approved, waived, blocked
   public string? SupersedesArtifactId { get; init; }
   public string? SupersededByArtifactId { get; init; }
   public required DateTimeOffset CreatedAt { get; init; }
   public required string CreatedBy { get; init; }
   public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// ISL v2.3 Sec 11.2: Artifact Admission Record Schema.
/// </summary>
public record ArtifactAdmissionRecord
{
   public required string AdmissionId { get; init; }
   public required string ArtifactId { get; init; }
   public required string AdmissionSource { get; init; } // agent, tool, repair, import
   public required string SourceReference { get; init; }
   public required string TaskId { get; init; }
   public required string RepositoryLocation { get; init; }
   public required string AdmissionOutcome { get; init; } // admitted, rejected, escalated
   public IReadOnlyList<string>? AdmissionFindings { get; init; }
   public required DateTimeOffset AdmittedAt { get; init; }
   public required string AdmittedBy { get; init; }
}

/// <summary>
/// ISL v2.3 Sec 12.2: Promotion Record Schema.
/// </summary>
public record ArtifactPromotionRecord
{
   public required string PromotionId { get; init; }
   public required string ArtifactId { get; init; }
   public required string FromState { get; init; }
   public required string ToState { get; init; }
   public required IReadOnlyList<string> ValidationResults { get; init; }
   public string? TraceabilitySnapshotId { get; init; }
   public required DateTimeOffset PromotedAt { get; init; }
   public required string PromotedBy { get; init; }
}

/// <summary>
/// ISL v2.3 Sec 19.2: Package Manifest Schema.
/// </summary>
public record PackageManifestRecord
{
   public required string PackageId { get; init; }
   public required string PackageType { get; init; } // build-package, deployment-bundle, etc.
   public required string PackageVersion { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required IReadOnlyList<string> ArtifactIds { get; init; }
   public required IReadOnlyList<string> ArtifactVersions { get; init; }
   public required IReadOnlyList<string> ValidationResults { get; init; }
   public required string TraceabilitySnapshotId { get; init; }
   public required string PackageLocation { get; init; }
   public required string PackageStatus { get; init; } // candidate, validated, authorized, deployed
   public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// ISL v2.3 Sec 18.2: Artifact Evidence Record Schema.
/// </summary>
public record ArtifactEvidenceRecord
{
   public required string EvidenceRecordId { get; init; }
   public required string ArtifactId { get; init; }
   public required string ArtifactVersion { get; init; }
   public required string EvidenceType { get; init; } // compile, test, scan, analysis
   public required string ValidationResultId { get; init; }
   public required string EvidenceLocation { get; init; }
   public required string Outcome { get; init; } // passed, failed, warning, waived
   public required DateTimeOffset CreatedAt { get; init; }
}

public record ArtifactContent
{
   public required string ArtifactId { get; init; }
   public required byte[] Payload { get; init; }
}
