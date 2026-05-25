using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Tools.Models;

/// <summary>
/// ISL v1.6 Sec 13.1: Tool Finding Schema
/// </summary>
public record ToolFinding
{
   public required string FindingId { get; init; } = $"FND-{Guid.NewGuid():N}";
   public required string Severity { get; init; } // critical, high, medium, low, informational
   public required string Category { get; init; }
   public required string Message { get; init; }
   public string? AffectedArtifactId { get; init; }
   public required string RecommendedAction { get; init; }
   public required bool BlocksProgression { get; init; }
}

/// <summary>
/// ISL v1.6 Sec 12.1: Tool Invocation Result Schema.
/// Represents the normalized result returned by a deterministic tool plugin [1].
/// </summary>
public record ToolInvocationResult
{
   public required string InvocationResultId { get; init; } = $"VRS-{Guid.NewGuid():N}";
   public required string InvocationId { get; init; }
   public required string ToolId { get; init; }
   public required string Capability { get; init; }

   // REQUIRED: Link back to the task that initiated the tool validation [1]
   public required string TaskId { get; init; }

   public required IReadOnlyList<string> ArtifactIds { get; init; }

   /// <summary>
   /// passed, failed, warning, timeout, error, cancelled, not-applicable
   /// </summary>
   public required string Outcome { get; init; }

   public IReadOnlyList<ToolFinding>? Findings { get; init; }
   public required int DurationMs { get; init; }

   public required string ToolVersionConfirmed { get; init; }
   public string? EnvironmentConfirmed { get; init; }
   public required string NormalizedBy { get; init; }

   public required DateTimeOffset StartedAt { get; init; }
   public required DateTimeOffset CompletedAt { get; init; }
   public required string NextAction { get; init; } // proceed, repair, retry, escalate, fail-task, halt
}
