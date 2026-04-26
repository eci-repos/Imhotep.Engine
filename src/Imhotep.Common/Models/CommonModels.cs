using System;
using System.Collections.Generic;

namespace Imhotep.Common.Models;

#region -- State and Memory Models (ISL v2.2)

/// <summary>
/// The five strict categories of operational state maintained by the IMHOTEP platform (ISL v2.2).
/// </summary>
public enum StateCategory
{
   Specification,
   Planning,
   Execution,
   Artifact,
   Governance
}

/// <summary>
/// Represents the real-time operational condition of a specific platform subsystem.
/// Utilized across the platform to ensure continuity.
/// </summary>
public record PlatformState
{
   public required string TransactionId { get; init; }
   public required StateCategory Category { get; init; }
   public required string CurrentStatus { get; init; }
   public required string SerializedContextPayload { get; init; }
   public DateTimeOffset LastUpdated { get; init; }
}

/// <summary>
/// Represents the historical, immutable record of past activities, decisions, and tool outputs.
/// Used by agents globally to establish context without hallucinating.
/// </summary>
public record MemoryRecord
{
   public required string RecordId { get; init; }
   public required string TransactionId { get; init; }
   public string? TaskId { get; init; }
   public required string ActorId { get; init; }
   public required string EventDescription { get; init; }
   public required string StructuredPayload { get; init; }
   public DateTimeOffset Timestamp { get; init; }
}

#endregion
#region -- Deterministic Validation Models (ISL v3.9)


/// <summary>
/// Represents the bounded, enterprise-ready input passed to a deterministic engineering tool.
/// Extends the core constraints to support multi-file compilation and the full execution runtime memory models.
/// </summary>
public record ValidationRequest
{
   /// <summary>
   /// The active execution transaction orchestrating this validation (e.g., "TX-ACTIVE").
   /// Essential for the State and Memory Model to persist execution context.
   /// </summary>
   public required string TransactionId { get; init; }

   /// <summary>
   /// The specific construction task that generated the artifacts being evaluated.
   /// Essential for routing tool failures back to the Repair Analyst agent.
   /// </summary>
   public required string GeneratingTaskId { get; init; }

   /// <summary>
   /// The physical artifacts to be validated (e.g., Dictionary where Key = ArtifactId/FileName, Value = Content).
   /// Utilizing a dictionary allows tools like the .NET Compiler Plugin to evaluate multi-file dependencies at once.
   /// </summary>
   public required Dictionary<string, string> ArtifactContent { get; init; }

   /// <summary>
   /// The TraceabilityId of the specification entity these artifacts fulfill (e.g., "INT-001").
   /// </summary>
   public required string TargetTraceabilityId { get; init; }

   /// <summary>
   /// The specific deterministic Validation rule being executed (e.g., "VAL-001").
   /// Maps the request directly to the exact Tool Plugin required.
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


#endregion