using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Imhotep.SemanticModel.Services;


public class SemanticModelService : ISemanticModelService
{
   private readonly ILogger<SemanticModelService> _logger;

   // In a production enterprise deployment, this would be backed by a persistent graph 
   // or document database (e.g., Cosmos DB, Neo4j) to map trace links [5].
   private readonly Dictionary<string, CanonicalSemanticModel> _modelStore = new();

   public SemanticModelService(ILogger<SemanticModelService> logger)
   {
      _logger = logger;
   }

   public Task StoreModelAsync(CanonicalSemanticModel model, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("Storing Canonical Semantic Model for Transaction {TransactionId}", model.TransactionId);
      _modelStore[model.TransactionId] = model;
      return Task.CompletedTask;
   }

   public Task<CanonicalSemanticModel?> GetModelAsync(string transactionId, CancellationToken cancellationToken = default)
   {
      if (_modelStore.TryGetValue(transactionId, out var model))
      {
         return Task.FromResult<CanonicalSemanticModel?>(model);
      }

      _logger.LogWarning("Canonical Semantic Model for Transaction {TransactionId} not found.", transactionId);
      return Task.FromResult<CanonicalSemanticModel?>(null);
   }

   public async Task<ICanonicalEntity?> GetEntityByIdAsync(string transactionId, string traceabilityId, CancellationToken cancellationToken = default)
   {
      var model = await GetModelAsync(transactionId);
      if (model == null) return null;

      // Retrieves the strict canonical entity defined in the ISL blueprint
      var entity = model.AllEntities.FirstOrDefault(e => e.TraceabilityId == traceabilityId);

      if (entity == null)
      {
         _logger.LogWarning("Traceability Identifier {TraceabilityId} not found in Semantic Model for Transaction {TransactionId}", traceabilityId, transactionId);
      }

      return entity;
   }

   public async Task<IReadOnlyList<T>> GetEntitiesByTypeAsync<T>(string transactionId, CancellationToken cancellationToken = default) where T : class, ICanonicalEntity
   {
      var model = await GetModelAsync(transactionId);
      if (model == null) return new List<T>();

      return model.AllEntities.OfType<T>().ToList();
   }

   /// <summary>
   /// Safely retrieves a specific bounded subset of canonical entities by their Traceability Identifiers.
   /// Used by the Agent Orchestrator to isolate agent context windows.
   /// </summary>
   public async Task<IReadOnlyList<ICanonicalEntity>> GetEntitiesByIdsAsync(
       string transactionId,
       IEnumerable<string> entityIds,
       CancellationToken cancellationToken = default)
   {
      // 1. Enforce Context Minimization by returning early if no IDs are requested
      if (entityIds == null || !entityIds.Any())
      {
         return new List<ICanonicalEntity>();
      }

      // 2. Fetch the active canonical model using your existing async method
      var activeModel = await GetModelAsync(transactionId, cancellationToken);

      if (activeModel == null)
      {
         // If the model cannot be found, return empty to prevent null-reference exceptions downstream
         return new List<ICanonicalEntity>();
      }

      var resolvedEntities = new List<ICanonicalEntity>();

      // 3. Leverage the existing synchronous GetEntityById method on the CanonicalSemanticModel POCO
      foreach (var id in entityIds.Distinct())
      {
         // Enforce cancellation token to support execution halting (the digital "Andon Cord")
         cancellationToken.ThrowIfCancellationRequested();

         var entity = activeModel.GetEntityById(id);
         if (entity != null)
         {
            resolvedEntities.Add(entity);
         }
      }

      return resolvedEntities;
   }

   public async Task<IReadOnlyList<ICanonicalEntity>> GetEntitiesByConstraintAsync(string transactionId, string targetTraceabilityId, string relationshipType, CancellationToken cancellationToken = default)
   {
      // E.g., Find all 'Validations' that explicitly "Verify" POL-CJIS-001
      var model = await GetModelAsync(transactionId);
      if (model == null) return new List<ICanonicalEntity>();

      var edges = model.RelationshipEdge
          .Where(e => e.TargetId == targetTraceabilityId && e.RelationshipType == relationshipType)
          .Select(e => e.SourceId)
          .ToList();

      var relatedEntities = new List<ICanonicalEntity>();
      foreach (var id in edges)
      {
         var entity = await GetEntityByIdAsync(transactionId, id);
         if (entity != null)
         {
            relatedEntities.Add(entity);
         }
      }

      return relatedEntities;
   }

}
