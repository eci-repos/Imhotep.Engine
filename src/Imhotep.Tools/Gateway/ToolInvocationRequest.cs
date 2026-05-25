using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Tools.Gateway;

/// <summary>
/// Represents a formal request to invoke a deterministic engineering tool 
/// (ISL v1.6 Section 13.1: Tool Invocation Request Schema).
/// </summary>
public record ToolInvocationRequest
{
   public string ToolInvocationId { get; init; } = $"TIV-{Guid.NewGuid():N}";
   public required string ToolSelectionId { get; init; }
   public required string ToolPluginId { get; init; }
   public required string PluginVersion { get; init; }
   public required string ToolName { get; init; }
   public required string ToolVersion { get; init; }
   public required string CapabilityName { get; init; }

   public required string TaskId { get; init; }
   public string? WorkItemId { get; init; }
   public string? ExecutionGraphId { get; init; }

   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }

   public IReadOnlyList<string>? ArtifactIds { get; init; }
   public IReadOnlyList<string>? ArtifactVersionIds { get; init; }
   public required IReadOnlyList<string> InputReferences { get; init; }

   public object? Parameters { get; init; }
   public required string EnvironmentProfileId { get; init; }
   public required string IsolationProfileId { get; init; }

   public required int TimeoutSeconds { get; init; }
   public required bool DryRun { get; init; }
   public string? GovernanceCheckId { get; init; }
   public required string CorrelationId { get; init; }

   public required DateTimeOffset RequestedAt { get; init; }
   public required string RequestedBy { get; init; }
}
