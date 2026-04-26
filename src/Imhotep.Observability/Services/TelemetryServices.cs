using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Imhotep.Observability.Models;

namespace Imhotep.Observability.Services;

/// <summary>
/// Collects telemetry and exposes operational metrics for monitoring and analysis by Human Governance Roles.
/// </summary>
public interface ITelemetryService
{
   void RecordEvent(ITelemetryEvent telemetryEvent);

   /// <summary>
   /// Retrieves the telemetry stream for a specific blueprint transaction to power the Watchtower operational dashboards.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId);
}

/// <summary>
/// The Watchtower subsystem providing operational visibility into the autonomous SDLC (ISL v2.6).
/// </summary>
public class TelemetryService : ITelemetryService
{
   private readonly ILogger<TelemetryService> _logger;

   // In a production enterprise deployment, this would push to a time-series database 
   // or observability sink like Elasticsearch, Datadog, or Azure Application Insights.
   private readonly List<ITelemetryEvent> _telemetryStore = new();

   public TelemetryService(ILogger<TelemetryService> logger)
   {
      _logger = logger;
   }

   public void RecordEvent(ITelemetryEvent telemetryEvent)
   {
      _telemetryStore.Add(telemetryEvent);

      // Log output dynamically based on the strongly-typed record
      _logger.LogInformation("Telemetry Recorded: [{EventType}] TX: {TransactionId} at {Time}",
          telemetryEvent.GetType().Name, telemetryEvent.TransactionId, telemetryEvent.Timestamp);

      // 1. Proactive Monitoring & Alerting (ISL v2.6)
      EvaluateForAnomalies(telemetryEvent);
   }

   public IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId)
   {
      return _telemetryStore
          .Where(e => e.TransactionId == transactionId)
          .OrderBy(e => e.Timestamp)
          .ToList();
   }

   /// <summary>
   /// Analyzes real-time telemetry to detect abnormal patterns that require human intervention.
   /// </summary>
   private void EvaluateForAnomalies(ITelemetryEvent telemetryEvent)
   {
      switch (telemetryEvent)
      {
         case ExecutionTelemetry exec when exec.ExecutionStatus.Equals("InRepair", StringComparison.OrdinalIgnoreCase):
            // Detect if a task is trapped in excessive repair loops
            var repairCount = _telemetryStore.OfType<ExecutionTelemetry>()
                .Count(e => e.TaskId == exec.TaskId && e.ExecutionStatus.Equals("InRepair", StringComparison.OrdinalIgnoreCase));

            if (repairCount >= 3)
            {
               _logger.LogWarning("WATCHTOWER ALERT: Task {TaskId} has entered {Count} repair cycles. Escalation imminent.",
                   exec.TaskId, repairCount);
            }
            break;

         case ToolInteractionTelemetry tool when !tool.IsSuccessful:
            // Detect failures from critical deterministic tools (e.g., PII Scanners or Compilers)
            _logger.LogWarning("WATCHTOWER ALERT: Deterministic Tool '{ToolName}' failed validation for TraceabilityId: {TraceId}",
                tool.ToolName, tool.TargetTraceabilityId);
            break;

         case GovernanceTelemetry gov when gov.ApprovalGateStatus.Equals("Escalated", StringComparison.OrdinalIgnoreCase):
            // Instantly flag when a Human-Machine Escalation ("Andon Cord") is pulled
            _logger.LogCritical("WATCHTOWER ESCALATION: Policy {PolicyId} triggered a structural conflict requiring Human Governance intervention.",
                gov.PolicyId);
            break;
      }
   }
}
