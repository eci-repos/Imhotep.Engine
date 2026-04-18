using System.Collections.Generic;

namespace Imhotep.Tools.Models;

/// <summary>
/// Represents the bounded input passed to a deterministic engineering tool.
/// </summary>
public record ValidationRequest
{
   /// <summary>
   /// The physical artifact (e.g., C# code, JSON payload) to be validated.
   /// </summary>
   public required string ArtifactContent { get; init; }

   /// <summary>
   /// The TraceabilityId of the specification entity this artifact fulfills (e.g., "INT-001").
   /// </summary>
   public required string TargetTraceabilityId { get; init; }

   /// <summary>
   /// The specific deterministic Validation rule being executed (e.g., "VAL-001").
   /// </summary>
   public required string ValidationRuleId { get; init; }
}

/// <summary>
/// Represents the strict structured output contract returned by an external tool plugin.
/// This prevents raw, unstructured console text from breaking the autonomous repair loop.
/// </summary>
public record ValidationResult
{
   /// <summary>
   /// Indicates whether the deterministic tool execution passed successfully.
   /// </summary>
   public required bool IsSuccessful { get; init; }

   /// <summary>
   /// Specific compilation or structural errors detected by the tool.
   /// </summary>
   public IReadOnlyList<string> Errors { get; init; } = new List<string>();

   /// <summary>
   /// Specific security vulnerabilities or policy violations detected.
   /// </summary>
   public IReadOnlyList<string> SecurityFindings { get; init; } = new List<string>();

   /// <summary>
   /// Traces the validation result back to the specific validation rule.
   /// </summary>
   public required string ValidationRuleId { get; init; }
}

