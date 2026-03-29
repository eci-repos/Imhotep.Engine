using System;
using System.Collections.Generic;
using System.Text;

using Imhotep.SemanticModel.Graph;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Evaluation;

public interface IReadinessEvaluator
{
   /// <summary>
   /// Performs automated structural analysis to verify consistency and clear Approval Gates.
   /// </summary>
   Task<SpecificationReadinessReport> EvaluateReadinessAsync(
      CanonicalSemanticModel semanticModel, CancellationToken cancellationToken = default);
}
