using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.SemanticModel.Services;

/// <summary>
/// Manages the canonical representation of the system specification.
/// Acts as the central knowledge base of the platform, providing structured query interfaces 
/// used by the Planning Engine, Agent Orchestrator, and Traceability model [3].
/// </summary>
public interface ISemanticModelService
{
   /// <summary>
   /// Securely stores the successfully parsed CanonicalSemanticModel after it passes human Approval Gates [1].
   /// </summary>
   Task StoreModelAsync(CanonicalSemanticModel model);

   /// <summary>
   /// Retrieves the active Canonical Semantic Model representing the architectural blueprint.
   /// </summary>
   Task<CanonicalSemanticModel?> GetModelAsync(string transactionId);

   /// <summary>
   /// Queries the graph for a specific canonical entity using its persistent Traceability Identifier (e.g., "REQ-001") [4].
   /// </summary>
   Task<ICanonicalEntity?> GetEntityByIdAsync(string transactionId, string traceabilityId);

   /// <summary>
   /// Retrieves all entities of a specific canonical type (e.g., DataEntity, Policy, Service) 
   /// to support the Planning Engine and Agent Context Assembly [1, 3].
   /// </summary>
   Task<IReadOnlyList<T>> GetEntitiesByTypeAsync<T>(string transactionId) where T : class, ICanonicalEntity;
   Task<IReadOnlyList<ICanonicalEntity>> GetEntitiesByConstraintAsync(
      string transactionId, string targetTraceabilityId, string relationshipType);
}

