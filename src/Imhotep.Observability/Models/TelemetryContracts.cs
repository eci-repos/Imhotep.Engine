
using System;
using System.Collections.Generic;

namespace Imhotep.Observability.Models;

/// <summary>
/// Base contract for all telemetry events captured by the platform to support observability.
/// </summary>
public interface ITelemetryEvent
{
   string EventId { get; init; }
   DateTimeOffset Timestamp { get; init; }
   string TransactionId { get; init; }
}

/// <summary>
/// Tracks the real-time progression of the Construction Task Graph within the execution runtime.
/// </summary>
public record ExecutionTelemetry : ITelemetryEvent
{
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }

   /// <summary>
   /// The specific construction task being tracked (e.g., generating a service or executing a repair).
   /// </summary>
   public required string TaskId { get; init; }

   /// <summary>
   /// The current status of the task (e.g., "Initialized", "InRepair", "Completed").
   /// </summary>
   public required string ExecutionStatus { get; init; }
}

/// <summary>
/// Records the bounded reasoning activities of specialized agents.
/// </summary>
public record AgentActivityTelemetry : ITelemetryEvent
{
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }

   /// <summary>
   /// The specific agent invoked (e.g., "Implementation Generator", "Repair Analyst").
   /// </summary>
   public required string AgentRole { get; init; }

   /// <summary>
   /// The exact task assigned to the agent.
   /// </summary>
   public required string TaskId { get; init; }
}

/// <summary>
/// Streams deterministic feedback directly from the Tool Plugin Architecture.
/// </summary>
public record ToolInteractionTelemetry : ITelemetryEvent
{
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }

   /// <summary>
   /// The deterministic tool executed (e.g., "Schema Validation Plugin", "Static Analysis Security Scanner").
   /// </summary>
   public required string ToolName { get; init; }

   /// <summary>
   /// The TraceabilityId of the constraint being verified (e.g., "VAL-001").
   /// </summary>
   public required string TargetTraceabilityId { get; init; }

   /// <summary>
   /// Indicates whether the artifact passed deterministic validation.
   /// </summary>
   public required bool IsSuccessful { get; init; }
}

/// <summary>
/// Logs all real-time compliance checks and automated policy enforcement decisions.
/// </summary>
public record GovernanceTelemetry : ITelemetryEvent
{
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }

   /// <summary>
   /// The policy constraint evaluated (e.g., "POL-CJIS-001").
   /// </summary>
   public required string PolicyId { get; init; }

   /// <summary>
   /// The result of the policy check or Approval Gate (e.g., "Approved", "Escalated").
   /// </summary>
   public required string ApprovalGateStatus { get; init; }
}

