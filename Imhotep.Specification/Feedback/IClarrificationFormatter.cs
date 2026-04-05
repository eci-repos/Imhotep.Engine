using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Imhotep.SemanticModel.Graph;

using Imhotep.SemanticModel.Feedback;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Feedback;

/// <summary>
/// Defines the contract for formatting readiness evaluation gaps into the 
/// strict Human-Machine escalation format required by the IMHOTEP architecture.
/// </summary>
public interface IClarificationFormatter
{
   /// <summary>
   /// Transforms a failed SpecificationReadinessReport into a formatted clarification block.
   /// </summary>
   string FormatClarifications(SpecificationReadinessReport report);
}
