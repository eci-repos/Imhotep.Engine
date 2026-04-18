
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Imhotep.Observability.Models;
using Imhotep.Observability.Services;

namespace Imhotep.TelemetryService.Monitoring;

/// <summary>
/// A background worker that analyzes the telemetry stream in real-time to detect 
/// operational anomalies, excessive repair loops, or critical governance alerts.
/// </summary>
public class WatchtowerMonitor : BackgroundService
{
   private readonly ITelemetryService _telemetryService;
   private readonly ILogger<WatchtowerMonitor> _logger;

   public WatchtowerMonitor(ITelemetryService telemetryService, ILogger<WatchtowerMonitor> logger)
   {
      _telemetryService = telemetryService;
      _logger = logger;
   }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      _logger.LogInformation("Watchtower Monitor activated. Scanning for operational anomalies...");

      while (!stoppingToken.IsCancellationRequested)
      {
         // Simulate periodic analysis of the telemetry stream
         await Task.Delay(5000, stoppingToken);

         // In MACS POC, we pull recent events to check for anomalies like PII failures
         // that might trigger the human-machine escalation "Andon Cord".
         AnalyzeRecentTelemetry();
      }
   }

   private void AnalyzeRecentTelemetry()
   {
      // Example Rule: Detect if a deterministic tool is repeatedly failing, 
      // indicating that the Repair Analyst is stuck in an infinite loop.
      var allEvents = _telemetryService.GetTelemetryStream("TASK-SPEC-INIT-004"); // MACS POC TxID

      var recentToolFailures = allEvents.OfType<ToolInteractionTelemetry>()
                                        .Where(t => !t.IsSuccessful && t.ToolName == "PIIScannerPlugin")
                                        .Count();

      if (recentToolFailures >= 3)
      {
         _logger.LogCritical("WATCHTOWER ALERT: PII Scanner has failed {Count} consecutive times. " +
                             "Automated repair loop inefficient. Human escalation required.", recentToolFailures);

         // In a full implementation, this triggers an alert to the Operational Dashboard 
         // notifying the Security Validators and Court Auditors.
      }
   }
}

