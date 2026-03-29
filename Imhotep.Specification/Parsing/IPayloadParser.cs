using System;
using System.Collections.Generic;
using System.Text;
using Imhotep.SemanticModel.Graph;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Parsing;

public interface IPayloadParser
{
   /// <summary>
   /// Parses the raw STP, extracting metadata and strictly demarcated canonical sections.
   /// </summary>
   Task<ParsedPayload> ParseAsync(string rawPayload, CancellationToken cancellationToken = default);
}
