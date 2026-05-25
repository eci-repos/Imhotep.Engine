

// -------------------------------------------------------------------------------------------------
namespace Imhotep.SemanticModel.Entities;

public enum EntityType
{
   Project,
   Context,
   Stakeholder,
   Actor,
   Capability,
   Requirement,
   Service,
   Interface,
   DataEntityModel,
   Workflow,
   Policy,
   Infrastructure,
   Validation
}

// 1. Project: Represents the root identity and high-level objectives.
/// <summary>
/// ISL v1.1 Sec 10.0: Project Entity.
/// Represents the root identity, high-level objectives, and lifecycle state of the specification.
/// Exactly one Project entity MUST exist in every canonical model.
/// </summary>
public record ProjectEntity : ICanonicalEntity
{
   // --- ISL v1.1 Sec 8.1: Base Entity Fields (Required for all ICanonicalEntity objects) ---

   public EntityType Type { get; init; } = EntityType.Project;

   // Note: Using 'Id' to match the standard ISL v1.1 Traceability Identifier format and previous LINQ queries
   public required string TraceabilityId { get; init; }

   public required string Name { get; init; }
   public required string Description { get; init; }
   public required string Version { get; init; }
   public IReadOnlyList<string>? Relationships { get; init; }
   public required string SourceSection { get; init; }
   public required string Status { get; init; } = "active"; // active, deprecated, superseded, draft
   public object? Metadata { get; init; }

   // --- ISL v1.1 Sec 10.2: Project-Specific Fields (The Root Graph Anchor) ---

   public required string SystemId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string IslVersion { get; init; }
   public required string Domain { get; init; }
   public required string Owner { get; init; }

   public required string ReadinessLevel { get; init; } // draft, reviewable, machine-valid, autonomous-ready

   // CONDITIONAL: Required before reaching Autonomous-Ready or when governance policies apply
   public string? RiskTier { get; init; }
   public string? GovernanceProfile { get; init; }

   public required DateTimeOffset Created { get; init; }
   public required DateTimeOffset LastModified { get; init; }
}

// 2. Context: Describes the environment in which the system operates.
public record ContextEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Context;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string Environment { get; init; } = string.Empty;
}

// 3. Stakeholder: Represents individuals or human governance roles.
public record StakeholderEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Stakeholder;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Role { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 4. Actor: Represents entities (users, systems) that interact directly with the system.
public record ActorEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Actor;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 5. Capability: Represents higher-level system functions.
public record CapabilityEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Capability;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 6. Requirement: Statements of system behavior, constraints, or compliance.
public record RequirementEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Requirement;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 7. Service: Logical deployable subsystems responsible for implementing capabilities.
public record ServiceEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Service;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 8. Interface: Specific communication boundaries (e.g., APIs).
public record InterfaceEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Interface;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 9. DataEntityModel: Structured information models and relationships.
public record DataEntityModel : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.DataEntityModel;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 10. Workflow: Step-by-step behavioral processes and state transitions.
public record WorkflowEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Workflow;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 11. Policy: Strict security constraints, compliance rules, and access controls.
public record PolicyEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Policy;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string ConstraintLevel { get; init; } = string.Empty; // e.g., Mandatory, Recommended, Optional
}

// 12. Infrastructure: Deployment targets, scaling strategies, and runtime environments.
public record InfrastructureEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Infrastructure;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 13. Validation: Deterministic verification mechanisms mapped to specific tools.
public record ValidationEntity : ICanonicalEntity
{
   public EntityType Type { get; set; } = EntityType.Validation;
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string ToolMapping { get; init; } = string.Empty;
}

/// <summary>
/// Represents an explicit relational link (edge) between two entities in the Traceability Graph.
/// For example, linking a Validation rule (SourceId) back to the Policy it verifies (TargetId) 
/// to enable automated impact analysis.
/// 
/// Serves as the mathematical link between specification entities, 
/// forming the bidirectional Traceability Graph.
/// </summary>
public record TraceabilityEdge
{
   /// <summary>
   /// The TraceabilityId of the upstream/source entity (e.g., "POL-CJIS-001").
   /// </summary>
   public required string SourceId { get; init; }

   /// <summary>
   /// The TraceabilityId of the downstream/target entity fulfilling the constraint (e.g., "VAL-001").
   /// </summary>
   public required string TargetId { get; init; }

   /// <summary>
   /// Describes the explicit edge creation type (e.g., "Fulfills", "Constrains", "Implements").
   /// </summary>
   public required string RelationshipType { get; init; }
}

