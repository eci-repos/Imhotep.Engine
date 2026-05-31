using Imhotep.SemanticModel.Graph;
using Imhotep.Specification.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Normalization;

public interface ISemanticNormalizer
{
   /// <summary>
   /// Constructs the in-memory relational graph and extracts Traceability Identifiers.
   /// </summary>
   Task<CanonicalSemanticModel> NormalizeAsync(
      StructuredSpecificationPayload parsedPayload, CancellationToken cancellationToken = default);
}
