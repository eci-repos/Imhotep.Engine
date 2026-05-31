using System;
using System.Collections.Generic;
using System.Text;
using Imhotep.SemanticModel.Graph;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Parsing;

/// <summary>
/// ISL v3.0: Defines the abstraction for translating a physical document into parseable sections.
/// </summary>
public interface IPayloadParser
{
   Task<ExtractedPayload> ParseAsync(string rawPayload, CancellationToken cancellationToken = default);
}