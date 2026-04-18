
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;

namespace Imhotep.TelemetryService.Core;

/// <summary>
/// The concrete implementation of the Watchtower's ingestion engine. 
/// Stores telemetry events persistently and broadcasts them to the operational dashboard.
/// </summary>
public class TelemetryIngestionService : ITelemetryService
{
   // In a production MACS environment, this would be a persistent time-series database or event store.
   private readonly ConcurrentBag<ITelemetryEvent> _eventStore = new();
   private readonly ILogger<TelemetryIngestionService> _logger;

   public TelemetryIngestionService(ILogger<TelemetryIngestionService> logger)
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
            var status = tool.IsSuccessful ? "PASSED" : "FAILED";
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
   }

   public IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId)
   {
      return _eventStore
          .Where(e => e.TransactionId == transactionId)
          .OrderBy(e => e.Timestamp)
          .ToList();
   }
}

