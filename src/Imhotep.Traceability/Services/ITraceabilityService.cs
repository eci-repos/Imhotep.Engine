
using System.Collections.Generic;
using System.Threading.Tasks;
using Imhotep.Traceability.Models;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Traceability.Services;

/// <summary>
/// Maintains the traceability graph linking specifications, tasks, and artifacts.
/// Serves as the memory of the system to support auditing and system evolution.
/// </summary>
public interface ITraceabilityService
{
   /// <summary>
   /// Records a new bidirectional link when the execution runtime generates an artifact.
   /// </summary>
   Task RecordArtifactDerivationAsync(ArtifactTraceabilityLink link);

   /// <summary>
   /// Bidirectional navigation: Finds all physical artifacts derived from a specific specification entity.
   /// Enables stakeholders to verify that the system was constructed according to the blueprint.
   /// </summary>
   Task<IReadOnlyList<string>> GetArtifactsForSpecificationAsync(string traceabilityId);

   /// <summary>
   /// Bidirectional navigation: Finds the original specification intent that justifies a physical artifact.
   /// </summary>
   Task<string> GetSpecificationForArtifactAsync(string artifactId);

   /// <summary>
   /// Day 2 Operations: Analyzes the Traceability Graph to determine exactly which 
   /// tasks and artifacts must be reconstructed when a specification entity is modified.
   /// </summary>
   Task<ImpactAnalysisResult> PerformImpactAnalysisAsync(string updatedTraceabilityId, CanonicalSemanticModel activeModel);
}

