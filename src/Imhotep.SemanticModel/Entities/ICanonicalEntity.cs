using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.SemanticModel.Entities;

/// <summary>
/// Base interface ensuring every canonical entity tracks its Traceability ID
/// to support the bidirectional Traceability Graph.
/// </summary>
public interface ICanonicalEntity
{
   /// <summary>
   /// The persistent Traceability Identifier (e.g., "PROJ-001", "REQ-001").
   /// </summary>
   string TraceabilityId { get; init; }

   /// <summary>
   /// The name or short title of the canonical entity.
   /// </summary>
   string Name { get; init; }

   /// <summary>
   /// The detailed description or constraints of the entity.
   /// </summary>
   string Description { get; init; }
}

