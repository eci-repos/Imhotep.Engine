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
/// The authoritative, in-memory relational graph representing the 13 canonical entities.
/// This is the exact model the Planning Engine uses to construct the task graph.
/// </summary>
public record CanonicalSemanticModel(
    string TransactionId,                      // Added for runtime tracking
    string TargetArchitecture,                 // Added for deployment targeting
    ProjectEntity Project,
    IReadOnlyList<ContextEntity> Contexts,
    IReadOnlyList<StakeholderEntity> Stakeholders,
    IReadOnlyList<ActorEntity> Actors,
    IReadOnlyList<CapabilityEntity> Capabilities,
    IReadOnlyList<RequirementEntity> Requirements,
    IReadOnlyList<ServiceEntity> Services,
    IReadOnlyList<InterfaceEntity> Interfaces,
    IReadOnlyList<DataEntityModel> DataEntities,
    IReadOnlyList<WorkflowEntity> Workflows,
    IReadOnlyList<PolicyEntity> Policies,
    IReadOnlyList<InfrastructureEntity> Infrastructures,
    IReadOnlyList<ValidationEntity> Validations,
    IReadOnlyList<TraceabilityEdge> TraceabilityEdges // Added to hold explicit relationships
);

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
