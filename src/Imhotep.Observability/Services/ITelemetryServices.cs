
using System.Collections.Generic;
using Imhotep.Observability.Models;

namespace Imhotep.Observability.Services;

/// <summary>
/// Collects telemetry and exposes operational metrics for monitoring and analysis by Human Governance Roles.
/// </summary>
public interface ITelemetryService
{
   /// <summary>
   /// Records a structured telemetry event generated during the autonomous development loop.
   /// </summary>
   void RecordEvent(ITelemetryEvent telemetryEvent);

   /// <summary>
   /// Retrieves the telemetry stream for a specific blueprint transaction to power the Watchtower operational dashboards.
   /// </summary>
   IReadOnlyList<ITelemetryEvent> GetTelemetryStream(string transactionId);
}

