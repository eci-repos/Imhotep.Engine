
using System;
using System.Collections.Generic;

namespace Imhotep.Observability.Models;

using System;

/// <summary>
/// Base contract for all telemetry events captured by the platform to support observability.
/// Strictly aligned with the ISL v2.6 Common Telemetry Event Schema.
/// </summary>
public interface ITelemetryEvent
{
   // --- Base Identity & Timing ---
   string EventId { get; init; }
   DateTimeOffset Timestamp { get; init; }

   // Legacy MACS POC identifier (maps logically to ExecutionGraphId or CorrelationId in v2.6)
   string TransactionId { get; init; }

   // --- ISL v2.6 REQUIRED Common Schema Fields ---

   /// <summary>Specific name of the event (e.g., 'tool-invocation-failed')</summary>
   string EventName { get; init; }

   /// <summary>e.g., execution, agent, model, tool, artifact, governance, traceability</summary>
   string EventCategory { get; init; }

   /// <summary>e.g., event, trace, span, metric, log, alert</summary>
   string SignalType { get; init; }

   /// <summary>critical, high, medium, low, informational</summary>
   string Severity { get; init; }

   /// <summary>Universal correlation identifier across distributed subsystems</summary>
   string CorrelationId { get; init; }

   /// <summary>The subsystem emitting the event (e.g., 'Imhotep.ToolGateway')</summary>
   string SourceSubsystem { get; init; }

   /// <summary>none, redacted, summarized, restricted</summary>
   string RedactionStatus { get; init; }

   /// <summary>transient, operational, audit-support, archival</summary>
   string RetentionClass { get; init; }

   // --- ISL v2.6 CONDITIONAL Enterprise Correlation Fields ---
   // These allow the Watchtower to filter streams across active multi-project deployments

   string ExecutionGraphId { get; init; }
   string SpecificationId { get; init; }
   string SpecificationVersion { get; init; }
   string TaskId { get; init; }
}

/// <summary>
/// Tracks the real-time progression of the Construction Task Graph within the execution runtime.
/// Aligns with ISL v2.6 Section 13.0 (Execution Telemetry).
/// </summary>
public record ExecutionTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "execution";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Execution-Specific Properties ---

   /// <summary>
   /// The current status of the task (e.g., "pending", "in-progress", "completed", "failed", "escalated").
   /// </summary>
   public required string ExecutionStatus { get; init; }

   /// <summary>
   /// The execution phase the task belongs to (e.g., "Artifact Generation", "Automated Repair").
   /// </summary>
   public string Phase { get; init; } = string.Empty;
}

/// <summary>
/// Records the bounded reasoning activities of specialized agents.
/// Aligns with ISL v2.6 Section 15.0 (Agent Telemetry).
/// </summary>
public record AgentActivityTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "agent";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Agent-Specific Properties ---

   /// <summary>
   /// The specific agent invoked (e.g., "ImplementationGenerator", "RepairAnalyst").
   /// </summary>
   public required string AgentRole { get; init; }

   /// <summary>
   /// Universal identifier tracking this specific invocation of the agent.
   /// </summary>
   public required string AgentInvocationId { get; init; }

   /// <summary>
   /// The identifier of the specific output generated by the agent.
   /// </summary>
   public string AgentOutputId { get; init; } = string.Empty;
}

/// <summary>
/// Streams deterministic feedback directly from the Tool Plugin Architecture.
/// Aligns with ISL v2.6 Section 17.0 (Tool Telemetry).
/// </summary>
public record ToolInteractionTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "tool";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Tool-Specific Properties ---

   public required string ToolName { get; init; }

   /// <summary>
   /// The strict capability invoked (e.g., "compile", "static-analysis", "unit-test").
   /// </summary>
   public required string Capability { get; init; }

   public required string TargetTraceabilityId { get; init; }

   /// <summary>
   /// Upgraded from 'bool IsSuccessful' to support ISL v1.6 strict outcomes: "passed", "failed", "warning", "timeout", "error".
   /// </summary>
   public required string Outcome { get; init; }

   /// <summary>
   /// Helps Watchtower dashboards determine if the tool was stuck or struggled.
   /// </summary>
   public long DurationMs { get; init; }
}

/// <summary>
/// Logs all real-time compliance checks and automated policy enforcement decisions.
/// Aligns with ISL v2.6 Section 19.0 (Governance Telemetry).
/// </summary>
public record GovernanceTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "governance";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Governance-Specific Properties ---

   public required string PolicyId { get; init; }

   /// <summary>
   /// Maps to the formal governance record (e.g., an applied waiver or an escalation ticket).
   /// </summary>
   public string GovernanceRecordId { get; init; } = string.Empty;

   public required string ApprovalGateStatus { get; init; }
}

/// <summary>
/// Tracks the creation, modification, validation, and packaging of artifacts.
/// Aligns with ISL v2.6 Section 18.0 (Artifact and Repository Telemetry).
/// </summary>
public record ArtifactLifecycleTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "artifact";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Artifact-Specific Properties ---

   public required string ArtifactId { get; init; }

   /// <summary>
   /// Tracks version bumps due to automated repair cycles or supersessions.
   /// </summary>
   public required string ArtifactVersion { get; init; }

   public required string LifecycleAction { get; init; }
}

/// <summary>
/// Measures computational resources, processing time, and model invocation latency.
/// Aligns with ISL v2.6 Section 22.0 (Performance and Resource Telemetry).
/// </summary>
public record PerformanceTelemetry : ITelemetryEvent
{
   // --- ISL v2.6 ITelemetryEvent Base Properties ---
   public required string EventId { get; init; }
   public required DateTimeOffset Timestamp { get; init; }
   public required string TransactionId { get; init; }
   public required string EventName { get; init; }
   public required string EventCategory { get; init; } = "performance";
   public required string SignalType { get; init; }
   public required string Severity { get; init; }
   public required string CorrelationId { get; init; }
   public required string SourceSubsystem { get; init; }
   public required string RedactionStatus { get; init; }
   public required string RetentionClass { get; init; }
   public required string ExecutionGraphId { get; init; }
   public required string SpecificationId { get; init; }
   public required string SpecificationVersion { get; init; }
   public required string TaskId { get; init; }

   // --- Performance-Specific Properties ---

   public required string ComponentName { get; init; }
   public required long ExecutionDurationMilliseconds { get; init; }

   /// <summary>
   /// E.g., "CPU", "Memory", "TokenUsage", "QueueLatency".
   /// </summary>
   public string ResourceMetricType { get; init; } = string.Empty;
}

