using System;
using System.Collections.Generic;
using System.Text;

using Imhotep.SemanticModel.Feedback;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Feedback;

public interface IClarificationFormatter
{
   /// <summary>
   /// Formats missing canonical elements or ambiguous rules strictly into 
   /// the ### CLARIFICATIONS REQUIRED block.
   /// </summary>
   string FormatClarifications(IEnumerable<ClarificationItem> gaps);
}
