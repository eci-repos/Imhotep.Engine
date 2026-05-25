using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.SemanticModel.Services;

/// <summary>
/// ISL v2.0 Section 9.0: Semantic Model Engine.
/// Manages the canonical representation of the system specification.
/// Acts as the central knowledge base of the platform, providing structured query interfaces 
/// used by the Planning Engine, Agent Orchestrator, and Traceability model.
/// </summary>
public interface ISemanticModelService
{
   /// <summary>
   /// Securely stores the successfully parsed CanonicalSemanticModel after it passes human Approval Gates.
   /// </summary>
   Task StoreModelAsync(CanonicalSemanticModel model, CancellationToken cancellationToken = default);

   /// <summary>
   /// Retrieves the active Canonical Semantic Model representing the architectural blueprint.
   /// </summary>
   Task<CanonicalSemanticModel?> GetModelAsync(string transactionId, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v1.1 Section 23.0: Queries the graph for a specific canonical entity using its persistent Traceability Identifier (e.g., "REQ-001").
   /// </summary>
   Task<ICanonicalEntity?> GetEntityByIdAsync(string transactionId, string traceabilityId, CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v3.4 Section 10.2: Context Package Assembly.
   /// Safely retrieves a specific bounded subset of canonical entities by their Traceability Identifiers.
   /// Used by the Agent Orchestrator to isolate agent context windows.
   /// </summary>
   Task<IReadOnlyList<ICanonicalEntity>> GetEntitiesByIdsAsync(string transactionId, IEnumerable<string> entityIds, CancellationToken cancellationToken = default);

   /// <summary>
   /// Retrieves all entities of a specific canonical type (e.g., DataEntity, Policy, Service) 
   /// to support the Planning Engine.
   /// </summary>
   Task<IReadOnlyList<T>> GetEntitiesByTypeAsync<T>(string transactionId, CancellationToken cancellationToken = default) where T : class, ICanonicalEntity;

   /// <summary>
   /// ISL v1.1 Section 24.0: Queries entities based on canonical relationship edges (e.g., "validates", "implements", "governs").
   /// </summary>
   Task<IReadOnlyList<ICanonicalEntity>> GetEntitiesByConstraintAsync(string transactionId, string targetTraceabilityId, string relationshipType, CancellationToken cancellationToken = default);
}

