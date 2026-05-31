using System;
using System.Collections.Generic;
using System.Text;

using Imhotep.SemanticModel.Entities;
using Imhotep.Contracts.Governance;

namespace Imhotep.SemanticModel.Graph;

/// <summary>
/// Represents the extracted structure of an ISL Structured Transaction Payload (STP).
/// </summary>
public record StructuredSpecificationPayload
{
   // --- YAML Frontmatter Metadata [ISL v3.8 Sec 5.1 & ISL v1.0 Sec 8.1] ---
   public required string TransactionId { get; init; }
   public required IReadOnlyList<string> AgentRoles { get; init; }
   public required string TargetArchitecture { get; init; }

   // ADDED: Identity and version metadata extracted from the frontmatter or header.
   // The SemanticNormalizer uses these to hydrate the CanonicalSemanticModel identity [1, 2].
   public string? SystemId { get; init; }
   public string? SpecificationVersion { get; init; }
   public string? IslVersion { get; init; }

   public required string RawContextAssembly { get; init; }

   // --- Extracted Canonical Sections ---
   // Key: Canonical Entity Name (e.g., "DataEntity", "Policy")
   // Value: The raw markdown content residing beneath that header
   public required IReadOnlyDictionary<string, string> ExtractedEntities { get; init; }
}

/// <summary>
/// ISL v1.1:
/// The authoritative, normalized representation of the system architecture.
/// This model is securely stored by the Semantic Model Service and exposed 
/// to downstream engines.
/// </summary>
public record CanonicalSemanticModel
{
   public required string TransactionId { get; init; } = String.Empty;       // Added for runtime tracking
   public required string TargetArchitecture { get; init; } = String.Empty;  // Added for deployment targeting

   // --- Required Versioning & Identity (ISL v1.1 Sec 10.0 & 28.1) ---

   /// <summary>
   /// The stable system identifier derived from the Project entity (e.g., "macs-greeting").
   /// </summary>
   public required string SystemId { get; init; }

   /// <summary>
   /// The authored specification version (e.g., "1.0.0").
   /// </summary>
   public required string Version { get; init; }

   /// <summary>
   /// The version of the canonical model schema.
   /// </summary>
   public required string ModelVersion { get; init; } = "1.0.0";

   // The 13 Canonical Entities
   public required ProjectEntity? Project { get; init; }
   public required IReadOnlyList<ContextEntity> Contexts { get; init; } = new List<ContextEntity>();
   public required IReadOnlyList<StakeholderEntity> Stakeholders { get; init; } = new List<StakeholderEntity>();
   public required IReadOnlyList<ActorEntity> Actors { get; init; } = new List<ActorEntity>();
   public required IReadOnlyList<CapabilityEntity> Capabilities { get; init; } = new List<CapabilityEntity>();
   public required IReadOnlyList<RequirementEntity> Requirements { get; init; } = new List<RequirementEntity>();
   public required IReadOnlyList<ServiceEntity> Services { get; init; } = new List<ServiceEntity>();
   public required IReadOnlyList<InterfaceEntity> Interfaces { get; init; } = new List<InterfaceEntity>();
   public required IReadOnlyList<DataEntityModel> DataEntities { get; init; } = new List<DataEntityModel>();
   public required IReadOnlyList<WorkflowEntity> Workflows { get; init; } = new List<WorkflowEntity>();
   public required IReadOnlyList<PolicyEntity> Policies { get; init; } = new List<PolicyEntity>();
   public required IReadOnlyList<InfrastructureEntity> Infrastructures { get; init; } = new List<InfrastructureEntity>();
   public required IReadOnlyList<ValidationEntity> Validations { get; init; } = new List<ValidationEntity>();

   // The Bidirectional Traceability Graph
   public required IReadOnlyList<TraceabilityEdge> RelationshipEdge { get; init; } = new List<TraceabilityEdge>();

   /// <summary>
   /// Searches across all 13 canonical collections to return the matching entity by its TraceabilityId.
   /// </summary>
   public ICanonicalEntity? GetEntityById(string targetTraceabilityId, CancellationToken cancellationToken = default)
   {
      if (string.IsNullOrWhiteSpace(targetTraceabilityId)) return null;

      // 1. Check the Root Project Entity
      if (Project?.TraceabilityId == targetTraceabilityId) return Project;

      // 2. Cascade through the Canonical Collections
      return
          (ICanonicalEntity?)Contexts.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Stakeholders.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Actors.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Capabilities.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Requirements.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Services.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Interfaces.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)DataEntities.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Workflows.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Policies.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Infrastructures.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId) ??
          (ICanonicalEntity?)Validations.FirstOrDefault(e => e.TraceabilityId == targetTraceabilityId);
   }

   /// <summary>
   /// A computed aggregation property that flattens all 13 canonical entity lists.
   /// This allows the Semantic Model Service to easily traverse the entire graph 
   /// to find any entity by its TraceabilityId.
   /// </summary>
   public IEnumerable<ICanonicalEntity> AllEntities
   {
      get
      {
         var all = new List<ICanonicalEntity>();

         if (Project != null) all.Add(Project);
         all.AddRange(Contexts);
         all.AddRange(Stakeholders);
         all.AddRange(Actors);
         all.AddRange(Capabilities);
         all.AddRange(Requirements);
         all.AddRange(Services);
         all.AddRange(Interfaces);
         all.AddRange(DataEntities);
         all.AddRange(Workflows);
         all.AddRange(Policies);
         all.AddRange(Infrastructures);
         all.AddRange(Validations);

         return all;
      }
   }
}

/// <summary>
/// Represents the formal state of the blueprint against the ISL Specification Readiness Levels.
/// </summary>
public record SpecificationReadinessReport
{
   public ReadinessLevel Level { get; init; }
   public IReadOnlyList<string> Exceptions { get; init; } // Aggregates all outstanding issues preventing progression
   public IReadOnlyList<string> MissingCanonicalElements { get; init; }
   public IReadOnlyList<string> UnmappedValidationRules { get; init; }
   public IReadOnlyList<string> ConflictingPolicies { get; init; }
}
