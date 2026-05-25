using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Agents.Models;

/// <summary>
/// ISL v2.1 Section 8.1: Agent Invocation Request Schema
/// A structured request from the platform to an agent asking it to perform a task.
/// </summary>
public record AgentInvocationRequest
{
   public string AgentInvocationId { get; init; } = Guid.NewGuid().ToString();
   public required string AgentRole { get; init; }
   public required string TaskId { get; init; }
   public string? ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string CanonicalModelVersion { get; init; }
   public required IReadOnlyList<string> SourceEntityIds { get; init; }

   /// <summary>
   /// interpret, plan, generate, review, repair, test, security-validate, deploy-prepare
   /// </summary>
   public required string InvocationPurpose { get; init; }

   public required string ContextPackageId { get; init; }

   /// <summary>
   /// Defines the ISL v3.8 Output Contract the agent must fulfill.
   /// </summary>
   public required string ExpectedOutputContract { get; init; }

   public required int TimeoutSeconds { get; init; }
   public required string SensitivityClassification { get; init; }
   public required string CorrelationId { get; init; }

   public required DateTimeOffset RequestedAt { get; init; }
   public required string RequestedBy { get; init; }
}
