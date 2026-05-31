using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Imhotep.Observability.Models;

namespace Imhotep.Adapters.Telemetry;

/// <summary>
/// Translates internal ISL v2.6 Telemetry Events into standard OpenTelemetry Traces and Logs.
/// Lives in the /adapters/telemetry boundary to prevent coupling core logic to OTel.
/// </summary>
public class OpenTelemetryExporterAdapter
{
   // ActivitySource is the native .NET mechanism for emitting OpenTelemetry Traces and Spans
   private static readonly ActivitySource ImhotepActivitySource = new("Imhotep.ExecutionRuntime");

   private readonly ILogger<OpenTelemetryExporterAdapter> _logger;

   public OpenTelemetryExporterAdapter(ILogger<OpenTelemetryExporterAdapter> logger)
   {
      _logger = logger;
   }

   /// <summary>
   /// Converts an internal ITelemetryEvent into an OpenTelemetry Span and Log.
   /// </summary>
   public void ExportEventToOpenTelemetry(ITelemetryEvent telemetryEvent)
   {
      // 1. ISL v2.6 Sec 28.1: Map Agent, Tool, and Task Invocations to OpenTelemetry Spans
      using var activity = ImhotepActivitySource.StartActivity(telemetryEvent.EventName, ActivityKind.Internal);

      if (activity != null)
      {
         // 2. Map ISL v2.6 Base Correlation Fields to formal 'isl.' OpenTelemetry Semantic Conventions
         activity.SetTag("isl.event.id", telemetryEvent.EventId);
         activity.SetTag("isl.correlation.id", telemetryEvent.CorrelationId);
         activity.SetTag("isl.source.subsystem", telemetryEvent.SourceSubsystem);
         activity.SetTag("isl.execution.graph.id", telemetryEvent.ExecutionGraphId);
         activity.SetTag("isl.specification.id", telemetryEvent.SpecificationId);
         activity.SetTag("isl.specification.version", telemetryEvent.SpecificationVersion);
         activity.SetTag("isl.task.id", telemetryEvent.TaskId);
         activity.SetTag("isl.severity", telemetryEvent.Severity);

         // 3. Pattern match specific records to map specialized attributes
         switch (telemetryEvent)
         {
            case ToolInteractionTelemetry tool:
               activity.SetTag("isl.tool.name", tool.ToolName);
               activity.SetTag("isl.tool.capability", tool.Capability);
               activity.SetTag("isl.outcome", tool.Outcome);

               // Mark the span as failed in the OTel dashboard if the tool did not pass
               if (tool.Outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                   tool.Outcome.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                   tool.Outcome.Equals("timeout", StringComparison.OrdinalIgnoreCase))
               {
                  activity.SetStatus(ActivityStatusCode.Error, $"Tool resulted in {tool.Outcome}");
               }
               else
               {
                  activity.SetStatus(ActivityStatusCode.Ok);
               }
               break;

            case GovernanceTelemetry gov:
               activity.SetTag("isl.governance.policy.id", gov.PolicyId);
               activity.SetTag("isl.governance.gate.status", gov.ApprovalGateStatus);
               break;

            case AgentActivityTelemetry agent:
               activity.SetTag("isl.agent.role", agent.AgentRole);
               activity.SetTag("isl.agent.invocation.id", agent.AgentInvocationId);
               break;

            case ExecutionTelemetry exec:
               activity.SetTag("isl.execution.status", exec.ExecutionStatus);
               activity.SetTag("isl.execution.phase", exec.Phase);
               break;
         }
      }

      // 4. ISL v2.6 Sec 28.1: Map Telemetry Events to OpenTelemetry Log Records
      // By wrapping this in a logging scope, the OpenTelemetry Logs exporter will automatically 
      // attach these 'isl.' correlation identifiers to the exported log payload.
      using (_logger.BeginScope(new Dictionary<string, object>
      {
         ["isl.correlation.id"] = telemetryEvent.CorrelationId,
         ["isl.task.id"] = telemetryEvent.TaskId,
         ["isl.execution.graph.id"] = telemetryEvent.ExecutionGraphId
      }))
      {
         // Redaction control: Check retention class before logging sensitive payload data
         if (telemetryEvent.RedactionStatus.Equals("none", StringComparison.OrdinalIgnoreCase))
         {
            _logger.LogInformation("OTel Export: [{Category}] {EventName} recorded.",
                telemetryEvent.EventCategory, telemetryEvent.EventName);
         }
         else
         {
            _logger.LogInformation("OTel Export: [{Category}] {EventName} recorded. Payload redacted.",
                telemetryEvent.EventCategory, telemetryEvent.EventName);
         }
      }
   }
}
