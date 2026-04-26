using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Agents.Models;

/// <summary>
/// Represents the operational context provided to an agent during invocation (ISL v3.4).
/// </summary>
public class AgentContext
{
   public CanonicalSemanticModel SemanticModel { get; set; }
   public Dictionary<string, string> PriorArtifacts { get; set; } = new Dictionary<string, string>();

   // Injected by the Execution Runtime if the task is in a repair cycle
   public string DeterministicValidationFeedback { get; set; }

   /// <summary>
   /// The TraceabilityId of the specific deterministic validation rule that failed (e.g., "VAL-001").
   /// This strictly grounds the Repair Analyst to the exact source of the failure.
   /// </summary>
   public string ValidationRuleId { get; set; }
}

/// <summary>
/// Represents the strict structured output returned by an agent (ISL v3.8).
/// </summary>
public class AgentResult
{
   public string TargetTraceabilityId { get; set; }
   public bool IsSuccess { get; set; }
   public Dictionary<string, string> GeneratedArtifacts { get; set; } = new Dictionary<string, string>();
   public string StructuredOutput { get; set; }
   public string ErrorMessage { get; set; }
}
