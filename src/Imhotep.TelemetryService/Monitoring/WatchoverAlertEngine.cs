using Imhotep.Governance.Models;
using Imhotep.Governance.Services;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.TelemetryService.Monitoring;


/// <summary>
/// The Enterprise Alert Engine (Watchtower). 
/// Continuously evaluates the telemetry stream across all active execution graphs 
/// to detect non-convergent repair loops, security violations, and governance anomalies.
/// </summary>
public class EnterpriseWatchtowerAlertEngine : BackgroundService
{
   private readonly ITelemetryService _telemetryService;
   private readonly IGovernanceService _governanceService; // Bridges alerts to actual runtime halts
   private readonly ILogger<EnterpriseWatchtowerAlertEngine> _logger;

   public EnterpriseWatchtowerAlertEngine(
       ITelemetryService telemetryService,
       IGovernanceService governanceService,
       ILogger<EnterpriseWatchtowerAlertEngine> logger)
   {
      _telemetryService = telemetryService;
      _governanceService = governanceService;
      _logger = logger;
   }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      _logger.LogInformation("[WATCHTOWER] Enterprise Alert Engine activated. Monitoring all active control planes.");

      while (!stoppingToken.IsCancellationRequested)
      {
         await Task.Delay(5000, stoppingToken);

         // Now awaiting the async evaluation
         await EvaluateEnterpriseAlertRulesAsync(stoppingToken);
      }
   }

   private async Task EvaluateEnterpriseAlertRulesAsync(CancellationToken cancellationToken)
   {
      try
      {
         // 1. Retrieve all telemetry events 
         var activeEvents = _telemetryService.GetAllActiveExecutionTelemetry();

         // 2. Group failures by Specification and Artifact (using the upgraded ITelemetryEvent fields)
         var toolFailuresByArtifact = activeEvents.OfType<ToolInteractionTelemetry>()
             .Where(t => t.Outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                         t.Outcome.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                         t.Outcome.Equals("timeout", StringComparison.OrdinalIgnoreCase))
             .GroupBy(t => new { t.SpecificationId, t.SpecificationVersion, t.TargetTraceabilityId });

         foreach (var failureGroup in toolFailuresByArtifact)
         {
            if (failureGroup.Count() >= 3)
            {
               string specId = failureGroup.Key.SpecificationId;
               string specVersion = failureGroup.Key.SpecificationVersion;
               string artifactId = failureGroup.Key.TargetTraceabilityId;

               _logger.LogCritical("[WATCHTOWER ESCALATION] Artifact '{ArtifactId}' failed 3 consecutive times. Pulling Andon Cord.", artifactId);

               // ---> FIXED: Constructing your formal ISL v1.7 EscalationPayload <---
               var payload = new EscalationPayload
               {
                  SpecificationId = specId,
                  SpecificationVersion = specVersion,
                  TargetId = artifactId,
                  EscalationType = "repair", // ISL v1.7 escalation type for non-convergent loops
                  RequiredRole = "IT Architect", // Or "Security Validator" based on the tool
                  Severity = "blocking",
                  FailureContext = $"Automated repair loop non-convergent. Tool execution failed 3 consecutive times for artifact {artifactId}.",
                  TraceabilityPath = failureGroup.Select(f => f.EventId).ToList(), // Passing the specific event IDs as proof
                  RepairHistory = new List<string>() // Can be populated with specific agent reasoning IDs
               };

               // ---> FIXED: Calling the exact method on your IGovernanceService <---
               await _governanceService.OpenEscalationAsync(payload, cancellationToken);
            }
         }
      }
      catch (Exception ex)
      {
         _logger.LogError("[WATCHTOWER FAILURE] Alert evaluation failed: {Message}", ex.Message);
      }
   }

}
