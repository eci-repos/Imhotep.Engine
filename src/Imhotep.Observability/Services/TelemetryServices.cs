using Imhotep.Observability.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Imhotep.Observability.Services;

/// <summary>
/// Collects telemetry and exposes operational metrics for monitoring and analysis by Human Governance Roles.
/// Strictly aligned with the ISL v2.6 Observability and Telemetry Model.
/// </summary>
public interface ITelemetryService
{
   /// <summary>
   /// Emits a structured telemetry event into the Observability layer.
   /// </summary>
   void RecordEvent(ITelemetryEvent telemetryEvent);

   /// <summary>
   /// Legacy MACS POC Support: Retrieves the telemetry stream for a specific blueprint transaction.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId);

   // ---> ADDED: ISL v2.6 Enterprise Query Capabilities <---

   /// <summary>
   /// Retrieves all telemetry events across currently active execution graphs. 
   /// Required by the WatchtowerAlertEngine to detect non-convergent repair loops across multiple active projects.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetAllActiveExecutionTelemetry();

   /// <summary>
   /// Retrieves telemetry filtered by the active Execution Graph.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetTelemetryByExecutionGraph(string executionGraphId);

   /// <summary>
   /// Retrieves telemetry across all projects within a specified operational time window.
   /// Required by Human Governance roles for periodic operational audits.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetTelemetryByTimeRange(DateTimeOffset startTime, DateTimeOffset endTime);
}

/// <summary>
/// Collects telemetry and exposes operational metrics for monitoring and analysis by Human Governance Roles.
/// Strictly aligned with the ISL v2.6 Observability and Telemetry Model.
/// </summary>
public class TelemetryService : ITelemetryService
{
   private readonly ILogger<TelemetryService> _logger;

   // Swapped List for ConcurrentBag to support thread-safe, highly concurrent 
   // event streaming from distributed runtime workers (ISL v2.7).
   private readonly ConcurrentBag<ITelemetryEvent> _eventStore = new();

   public TelemetryService(ILogger<TelemetryService> logger)
   {
      _logger = logger;
   }

   public void RecordEvent(ITelemetryEvent telemetryEvent)
   {
      _eventStore.Add(telemetryEvent);

      // Pattern matching to log specific details based on the strict event schema
      switch (telemetryEvent)
      {
         case ExecutionTelemetry exec:
            _logger.LogInformation("[EXECUTION] Task: {TaskId} | Status: {Status} | TxID: {TxId}",
                exec.TaskId, exec.ExecutionStatus, exec.TransactionId);
            break;
         case ToolInteractionTelemetry tool:
            // Using the strict ISL v1.6 Outcome string
            // We convert it to uppercase (e.g., "TIMEOUT", "FAILED", "PASSED") to maintain your log formatting
            var status = tool.Outcome.ToUpperInvariant();
            _logger.LogInformation("[TOOL] {ToolName} verified {TargetTraceabilityId} | Result: {Status}",
                tool.ToolName, tool.TargetTraceabilityId, status);
            break;
         case GovernanceTelemetry gov:
            _logger.LogWarning("[GOVERNANCE] Policy: {PolicyId} | Gate Status: {Status}",
                gov.PolicyId, gov.ApprovalGateStatus);
            break;
         case AgentActivityTelemetry agent:
            _logger.LogInformation("[AGENT] Role: {AgentRole} assigned to Task: {TaskId}",
                agent.AgentRole, agent.TaskId);
            break;
      }

      // Proactive Monitoring & Alerting (ISL v2.6)
      EvaluateForAnomalies(telemetryEvent);
   }

   public IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId)
   {
      return _eventStore
          .Where(e => e.TransactionId == transactionId)
          .OrderBy(e => e.Timestamp)
          .ToList();
   }

   // Enterprise ISL v2.6 Query Capabilities

   public IReadOnlyList<ITelemetryEvent> GetAllActiveExecutionTelemetry()
   {
      // Feeds the EnterpriseWatchtowerAlertEngine, allowing it to scan across all currently active system builds.
      return _eventStore
          .OrderBy(e => e.Timestamp)
          .ToList();
   }

   public IReadOnlyList<ITelemetryEvent> GetTelemetryByExecutionGraph(string executionGraphId)
   {
      // ISL v2.6 Sec 25.1: Allows isolating dashboards to a specific multi-tenant execution graph
      return _eventStore
          .Where(e => e.ExecutionGraphId == executionGraphId)
          .OrderBy(e => e.Timestamp)
          .ToList();
   }

   public IReadOnlyList<ITelemetryEvent> GetTelemetryByTimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
   {
      // Required by Court Auditors and Security Validators for periodic operational audits
      return _eventStore
          .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
          .OrderBy(e => e.Timestamp)
          .ToList();
   }

   // Upgraded Anomaly Evaluation

   /// <summary>
   /// Analyzes real-time telemetry to detect abnormal patterns that require human intervention.
   /// </summary>
   private void EvaluateForAnomalies(ITelemetryEvent telemetryEvent)
   {
      switch (telemetryEvent)
      {
         case ExecutionTelemetry exec when exec.ExecutionStatus.Equals("InRepair", StringComparison.OrdinalIgnoreCase):
            // Detect if a task is trapped in excessive repair loops
            var repairCount = _eventStore.OfType<ExecutionTelemetry>()
                .Count(e => e.TaskId == exec.TaskId && e.ExecutionStatus.Equals("InRepair", StringComparison.OrdinalIgnoreCase));

            if (repairCount >= 3)
            {
               _logger.LogWarning("WATCHTOWER ALERT: Task {TaskId} has entered {Count} repair cycles. Escalation imminent.",
                   exec.TaskId, repairCount);
            }
            break;

         case ToolInteractionTelemetry tool when
              tool.Outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
              tool.Outcome.Equals("error", StringComparison.OrdinalIgnoreCase) ||
              tool.Outcome.Equals("timeout", StringComparison.OrdinalIgnoreCase):

            // Detect failures, crashes, or timeouts from critical deterministic tools
            _logger.LogWarning("WATCHTOWER ALERT: Deterministic Tool '{ToolName}' resulted in '{Outcome}' for TraceabilityId: {TraceId}",
                tool.ToolName, tool.Outcome, tool.TargetTraceabilityId);
            break;

         case GovernanceTelemetry gov when gov.ApprovalGateStatus.Equals("Escalated", StringComparison.OrdinalIgnoreCase):
            // Instantly flag when a Human-Machine Escalation ("Andon Cord") is pulled
            _logger.LogCritical("WATCHTOWER ESCALATION: Policy {PolicyId} triggered a structural conflict requiring Human Governance intervention.",
                gov.PolicyId);
            break;
      }
   }

}

