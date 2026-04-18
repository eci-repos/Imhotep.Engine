

// -------------------------------------------------------------------------------------------------
namespace Imhotep.SemanticModel.Entities;

// 1. Project: Represents the root identity and high-level objectives.
public record ProjectEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string Domain { get; init; } = string.Empty;
}

// 2. Context: Describes the environment in which the system operates.
public record ContextEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string Environment { get; init; } = string.Empty;
}

// 3. Stakeholder: Represents individuals or human governance roles.
public record StakeholderEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Role { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 4. Actor: Represents entities (users, systems) that interact directly with the system.
public record ActorEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 5. Capability: Represents higher-level system functions.
public record CapabilityEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 6. Requirement: Statements of system behavior, constraints, or compliance.
public record RequirementEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 7. Service: Logical deployable subsystems responsible for implementing capabilities.
public record ServiceEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 8. Interface: Specific communication boundaries (e.g., APIs).
public record InterfaceEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 9. DataEntityModel: Structured information models and relationships.
public record DataEntityModel : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 10. Workflow: Step-by-step behavioral processes and state transitions.
public record WorkflowEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 11. Policy: Strict security constraints, compliance rules, and access controls.
public record PolicyEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
   public string ConstraintLevel { get; init; } = string.Empty; // e.g., Mandatory, Recommended, Optional
}

// 12. Infrastructure: Deployment targets, scaling strategies, and runtime environments.
public record InfrastructureEntity : ICanonicalEntity
{
   public string TraceabilityId { get; init; } = string.Empty;
   public string Name { get; init; } = string.Empty;
   public string Description { get; init; } = string.Empty;
}

// 13. Validation: Deterministic verification mechanisms mapped to specific tools.
public record ValidationEntity : ICanonicalEntity
{
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

