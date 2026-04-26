using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.Text;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Feedback;

/// <summary>
/// Concrete implementation of IClarificationFormatter.
/// Abandons conversational responses and enforces "Advisory Collaboration" 
/// by generating a strictly formatted ### CLARIFICATIONS REQUIRED markdown block.
/// </summary>
public class ClarificationFormatter : IClarificationFormatter
{
   public string FormatClarifications(SpecificationReadinessReport report)
   {
      if (report == null)
         throw new ArgumentNullException(nameof(report));

      // If the blueprint is ready for autonomous execution, no clarifications are needed.
      if (report.Level == ReadinessLevel.AutonomousReady)
      {
         return string.Empty;
      }

      var clarificationBlock = new StringBuilder();

      // Enforce the strict markdown header mandated by the Model Interaction Protocol
      clarificationBlock.AppendLine("### CLARIFICATIONS REQUIRED");

      // Aggregate all gaps identified during the IReadinessEvaluator phase
      var allGaps = new List<string>();

      if (report.MissingCanonicalElements != null && report.MissingCanonicalElements.Any())
      {
         allGaps.AddRange(report.MissingCanonicalElements);
      }

      if (report.UnmappedValidationRules != null && report.UnmappedValidationRules.Any())
      {
         allGaps.AddRange(report.UnmappedValidationRules);
      }

      if (report.ConflictingPolicies != null && report.ConflictingPolicies.Any())
      {
         allGaps.AddRange(report.ConflictingPolicies);
      }

      // If gaps exist, format them as a structured bulleted list for the human team
      if (allGaps.Any())
      {
         foreach (var gap in allGaps)
         {
            clarificationBlock.AppendLine($"* {gap}");
         }
      }
      else
      {
         // Failsafe in case IsAutonomousReady is false but no specific messages were provided
         clarificationBlock.AppendLine("* The specification has not met the Autonomous-Ready level. Please review the blueprint for missing canonical entities or unresolved trace links.");
      }

      return clarificationBlock.ToString().TrimEnd();
   }
}
