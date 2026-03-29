using System;
using System.Collections.Generic;
using System.Text;

using Imhotep.SemanticModel.Graph;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Normalization;

public interface ISemanticNormalizer
{
   /// <summary>
   /// Constructs the in-memory relational graph and extracts Traceability Identifiers.
   /// </summary>
   Task<CanonicalSemanticModel> NormalizeAsync(
      ParsedPayload parsedPayload, CancellationToken cancellationToken = default);
}
