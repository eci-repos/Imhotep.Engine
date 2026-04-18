using Imhotep.Planning.Services;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Planning.Models;

/// <summary>
/// Transforms the static architectural blueprint into an actionable, sequential development plan.
/// </summary>
public interface IPlanningEngine
{
   /// <summary>
   /// Analyzes the relational graph and trace links within the Semantic Model 
   /// to autonomously generate the Construction Task Graph.
   /// </summary>
   ConstructionTaskGraph GenerateTaskGraph(CanonicalSemanticModel semanticModel);
}
