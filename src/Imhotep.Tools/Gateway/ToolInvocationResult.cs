using System;
using System.Collections.Generic;

namespace Imhotep.Tools.Gateway;

/// <summary>
/// The normalized structured response returned after a tool plugin executes.
/// Aligned with ISL v3.9 Section 14.1 (Tool Invocation Result Schema), 
/// which extends ISL v1.6 for the Tool Plugin Architecture.
/// </summary>
public record ToolInvocationResult
{
   // Standard IMHOTEP identifier format for Validation Result Schemas
   public string ToolInvocationResultId { get; init; } = $"VRS-{Guid.NewGuid():N}";

   public required string ToolInvocationId { get; init; }
   public required string ToolPluginId { get; init; }
   public required string ToolVersion { get; init; }
   public required string CapabilityName { get; init; }

   // Retained for Execution Runtime task linking [2]
   public required string TaskId { get; init; }

   /// <summary>
   /// passed, failed, warning, timeout, error, cancelled, not-applicable
   /// </summary>
   public required string Outcome { get; init; }

   // Added per ISL v3.9 to capture process boundary results [1]
   public int? ExitCode { get; init; }

   public IReadOnlyList<ToolFinding>? Findings { get; init; }
   public IReadOnlyList<string>? EvidenceReferences { get; init; }

   // ISL v3.9 explicitly requires tracking exact artifact versions to guarantee reproducibility [1]
   public IReadOnlyList<string>? AffectedArtifactIds { get; init; }
   public IReadOnlyList<string>? EvaluatedArtifactVersionIds { get; init; }

   public string? NormalizedOutputReference { get; init; }
   public string? RawOutputReference { get; init; }

   public required int DurationMs { get; init; }
   public object? ResourceUsage { get; init; }
   public required DateTimeOffset StartedAt { get; init; }
   public required DateTimeOffset CompletedAt { get; init; }

   /// <summary>
   /// proceed, repair, retry, escalate, fail-task, halt, ignore-warning
   /// </summary>
   public required string NextAction { get; init; }

   // Preserved from ISL v1.6 base compatibility [2]
   public string? EnvironmentConfirmed { get; init; }
   public string? NormalizedBy { get; init; }
}

/// <summary>
/// Provides structured details for validation failures, security reviews, or audit findings 
/// (ISL v1.6 Section 15.1: Tool Finding Schema).
/// </summary>
public record ToolFinding
{
   public string ToolFindingId { get; init; } = Guid.NewGuid().ToString();
   public required string ToolInvocationResultId { get; init; }

   /// <summary>
   /// e.g., compile, test, lint, static-analysis, security, repository, policy
   /// </summary>
   public required string FindingCategory { get; init; }

   /// <summary>
   /// e.g., critical, high, medium, low, informational
   /// </summary>
   public required string Severity { get; init; }

   public string? FindingCode { get; init; }
   public required string Message { get; init; }

   public string? AffectedArtifactId { get; init; }
   public string? AffectedArtifactVersionId { get; init; }
   public string? LocationReference { get; init; }
   public string? RuleReference { get; init; }
   public string? EvidenceReference { get; init; }

   /// <summary>
   /// e.g., proceed, repair, reconfigure, waive, escalate, fail
   /// </summary>
   public required string RecommendedAction { get; init; }
   public required bool BlocksProgression { get; init; }
}

