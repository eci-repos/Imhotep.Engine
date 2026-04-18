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
   public string TransactionId { get; init; } = String.Empty;       // Added for runtime tracking
   public string TargetArchitecture { get; init; } = String.Empty;  // Added for deployment targeting

   // The 13 Canonical Entities
   public ProjectEntity? Project { get; init; }
   public IReadOnlyList<ContextEntity> Contexts { get; init; } = new List<ContextEntity>();
   public IReadOnlyList<StakeholderEntity> Stakeholders { get; init; } = new List<StakeholderEntity>();
   public IReadOnlyList<ActorEntity> Actors { get; init; } = new List<ActorEntity>();
   public IReadOnlyList<CapabilityEntity> Capabilities { get; init; } = new List<CapabilityEntity>();
   public IReadOnlyList<RequirementEntity> Requirements { get; init; } = new List<RequirementEntity>();
   public IReadOnlyList<ServiceEntity> Services { get; init; } = new List<ServiceEntity>();
   public IReadOnlyList<InterfaceEntity> Interfaces { get; init; } = new List<InterfaceEntity>();
   public IReadOnlyList<DataEntityModel> DataEntities { get; init; } = new List<DataEntityModel>();
   public IReadOnlyList<WorkflowEntity> Workflows { get; init; } = new List<WorkflowEntity>();
   public IReadOnlyList<PolicyEntity> Policies { get; init; } = new List<PolicyEntity>();
   public IReadOnlyList<InfrastructureEntity> Infrastructures { get; init; } = new List<InfrastructureEntity>();
   public IReadOnlyList<ValidationEntity> Validations { get; init; } = new List<ValidationEntity>();

   // The Bidirectional Traceability Graph
   public IReadOnlyList<TraceabilityEdge> TraceabilityGraph { get; init; } = new List<TraceabilityEdge>();
}

/// <summary>
/// Represents the formal state of the blueprint against the ISL Specification Readiness Levels.
/// </summary>
public record SpecificationReadinessReport(
    bool IsMachineValid,
    bool IsAutonomousReady,
    IReadOnlyList<string> MissingCanonicalElements,
    IReadOnlyList<string> UnmappedValidationRules,
    IReadOnlyList<string> ConflictingPolicies
);
