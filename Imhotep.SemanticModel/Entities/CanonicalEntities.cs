

// -------------------------------------------------------------------------------------------------
namespace Imhotep.SemanticModel.Entities;

/// <summary>
/// Base contract ensuring all canonical entities participate in the Traceability Graph.
/// </summary>
public interface ICanonicalEntity
{
   string TraceabilityId { get; init; }
}

// 1. Project: Represents the root identity and high-level objectives.
public record ProjectEntity(
   string TraceabilityId, string Name, string Description, string Domain) : ICanonicalEntity;

// 2. Context: Describes the environment, integrations, and operational boundaries.
public record ContextEntity
   (string TraceabilityId, string Description) : ICanonicalEntity;

// 3. Stakeholder: Human governance, oversight roles, and accountability structures.
public record StakeholderEntity(
   string TraceabilityId, string RoleName, string Responsibilities) : ICanonicalEntity;

// 4. Actor: Users, automated processes, or external systems interacting directly.
public record ActorEntity(
   string TraceabilityId, string Name, string Description) : ICanonicalEntity;

// 5. Capability: High-level business functions derived from groups of requirements.
public record CapabilityEntity(
   string TraceabilityId, string Name, string Description) : ICanonicalEntity;

// 6. Requirement: Specific functional, non-functional, and operational rules.
public record RequirementEntity(
   string TraceabilityId, string Description, 
   IReadOnlyList<string> FulfillsCapabilityIds) : ICanonicalEntity;

// 7. Service: Logical deployable subsystems responsible for implementing capabilities.
public record ServiceEntity(
   string TraceabilityId, string Name, string Description, 
   IReadOnlyList<string> ImplementsCapabilityIds, 
   IReadOnlyList<string> UsesDataEntityIds) : ICanonicalEntity;

// 8. Interface: Specific communication boundaries (APIs, messaging endpoints).
public record InterfaceEntity(
   string TraceabilityId, string Name, string Description, string ConnectsActorId, 
   string ToServiceId) : ICanonicalEntity;

// 9. DataEntity: Structured information models, attributes, and relationships.
public record DataEntityModel(
   string TraceabilityId, string SchemaName, string Description) : ICanonicalEntity;

// 10. Workflow: Step-by-step behavioral processes and state transitions.
public record WorkflowEntity(
   string TraceabilityId, string Name, string Description) : ICanonicalEntity;

// 11. Policy: Strict security constraints, privacy rules, and access control mechanisms (e.g., CJIS/NIST).
public record PolicyEntity(
   string TraceabilityId, string Name, string ComplianceTier, string Description, 
   IReadOnlyList<string> ConstrainsEntityIds) : ICanonicalEntity;

// 12. Infrastructure: The deployment expectations and runtime environments.
public record InfrastructureEntity(
   string TraceabilityId, string TargetEnvironment, string Description) : ICanonicalEntity;

// 13. Validation: Deterministic rules and exact testing scenarios mapped to specific tools.
public record ValidationEntity(
   string TraceabilityId, string Name, string TargetToolPlugin, string Description,
   IReadOnlyList<string> VerifiesEntityIds) : ICanonicalEntity;

