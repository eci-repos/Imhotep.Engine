using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.SemanticModel.Services;

/// <summary>
/// The central knowledge base providing structured access to the specification graph.
/// </summary>
public interface ISemanticModelService
{
   /// <summary>
   /// Accepts the normalized specification graph during the architectural handoff.
   /// </summary>
   void LoadModel(CanonicalSemanticModel model);

   /// <summary>
   /// Retrieves the active, canonical representation of the system.
   /// </summary>
   CanonicalSemanticModel GetActiveModel();

   /// <summary>
   /// Queries the traceability graph to find all downstream entities impacted by a specific upstream entity.
   /// </summary>
   IReadOnlyList<string> GetImpactedEntities(string sourceTraceabilityId);

   /// <summary>
   /// Queries the traceability graph to find all upstream constraints a specific entity fulfills.
   /// </summary>
   IReadOnlyList<string> GetFulfillingConstraints(string targetTraceabilityId);
}

