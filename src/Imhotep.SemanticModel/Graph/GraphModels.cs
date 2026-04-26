using System;
using System.Collections.Generic;
using System.Text;

using Imhotep.SemanticModel.Entities;

namespace Imhotep.SemanticModel.Graph;

/// <summary>
/// The raw parsed state after the Markdown/YAML has been read, but before deep semantic normalization.
/// </summary>
public record ParsedPayload(
    string TransactionId,
    IReadOnlyList<string> AgentRoles,
    string TargetArchitecture,
    string RawContextAssembly,
    string RawContent,
    IReadOnlyDictionary<string, string> ExtractedEntities
);

/// <summary>
/// The authoritative, normalized representation of the system architecture.
/// This model is securely stored by the Semantic Model Service and exposed 
/// to downstream engines.
/// </summary>
public record CanonicalSemanticModel
{
   public required string TransactionId { get; init; } = String.Empty;       // Added for runtime tracking
   public required string TargetArchitecture { get; init; } = String.Empty;  // Added for deployment targeting

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
   public ICanonicalEntity? GetEntityById(string targetTraceabilityId)
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
/// Represents the maturity and execution authorization state of a system specification (ISL v1.3).
/// </summary>
public enum ReadinessLevel
{
   /// <summary>
   /// Exploratory stage. The platform provides advisory assistance but does not construct the system [8].
   /// </summary>
   Draft,

   /// <summary>
   /// The blueprint is structurally defined and ready for evaluation by Human Governance Roles [9].
   /// </summary>
   Reviewable,

   /// <summary>
   /// The blueprint has passed schema validation and is normalized into the canonical semantic model [10].
   /// </summary>
   MachineValid,

   /// <summary>
   /// All Approval Gates are cleared. The platform is officially authorized to begin autonomous construction [11].
   /// </summary>
   AutonomousReady
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
