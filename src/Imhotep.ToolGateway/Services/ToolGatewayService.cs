using Imhotep.Tools.Gateway; 
using Imhotep.ToolGateway.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Imhotep.ToolGateway.Services;

/// <summary>
/// ISL v3.9 Sec 8.0: The Tool Gateway. 
/// Provides deterministic tool invocation, trust profile enforcement, and result normalization.
/// </summary>
public class ToolGatewayService : IToolGateway
{
   private readonly IToolRegistry _toolRegistry;
   private readonly ILogger<ToolGatewayService> _logger;

   public ToolGatewayService(IToolRegistry toolRegistry, ILogger<ToolGatewayService> logger)
   {
      _toolRegistry = toolRegistry;
      _logger = logger;
   }

   public async Task<ToolInvocationResult> ExecuteToolAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation("[TOOL GATEWAY] Routing capability '{CapabilityName}' for Task: {TaskId}",
          request.CapabilityName, request.TaskId);

      var stopwatch = Stopwatch.StartNew();
      var startedAt = DateTimeOffset.UtcNow;

      try
      {
         // 1. Tool Discovery & Selection (Via your new IToolRegistry)
         var plugin = _toolRegistry.GetPluginForCapability(request.CapabilityName);

         if (plugin == null)
         {
            stopwatch.Stop();
            return GenerateErrorResult(request, "GATEWAY-ROUTER", startedAt, (int)stopwatch.ElapsedMilliseconds,
                $"CRITICAL: No registered tool plugin found for capability {request.CapabilityName}.");
         }

         // ---> NEW: 2. Trust Profile & Environment Enforcement (ISL v3.9 Sec 18.1) <---
         // Evaluates if the tool is legally authorized to run in the target EnvironmentProfileId.
         if (!plugin.IsAuthorizedForEnvironment(request.EnvironmentProfileId))
         {
            stopwatch.Stop();

            // Log the security violation to trigger Watchtower observability
            _logger.LogError("[TOOL GATEWAY SECURITY BLOCK] Tool '{ToolId}' trust profile disallows use in Environment '{EnvironmentProfileId}'.",
                plugin.ToolId, request.EnvironmentProfileId);

            return GenerateErrorResult(request, plugin.ToolId, startedAt, (int)stopwatch.ElapsedMilliseconds,
                $"SECURITY BLOCK: Tool {plugin.ToolId} trust profile prohibits execution in environment profile {request.EnvironmentProfileId}.");
         }

         // 3. Sandboxed Tool Invocation (Calling your exact ExecuteAsync method)
         var result = await plugin.ExecuteAsync(request);
         stopwatch.Stop();

         _logger.LogInformation("[TOOL GATEWAY] Validation completed. Plugin: {ToolId} | Outcome: {Outcome}",
             plugin.ToolId, result.Outcome);

         // 4. Result Normalization
         // Ensures the NormalizedBy constraint is strictly stamped by the platform Gateway
         return result with { NormalizedBy = "ToolGatewayService" };
      }
      catch (Exception ex)
      {
         stopwatch.Stop();

         // Ensure internal tool crashes do not bring down the IMHOTEP runtime
         _logger.LogError(ex, "[TOOL GATEWAY] Execution boundary violation during {CapabilityName}.", request.CapabilityName);

         return GenerateErrorResult(request, "GATEWAY-EXCEPTION", startedAt, (int)stopwatch.ElapsedMilliseconds,
             $"Tool execution boundary violation: {ex.Message}");
      }
   }

   /// <summary>
   /// Standardizes error routing to ensure Execution Runtime can safely pull the Andon Cord.
   /// </summary>
   private ToolInvocationResult GenerateErrorResult(ToolInvocationRequest request, string toolId, DateTimeOffset startedAt, int durationMs, string errorMessage)
   {
      return new ToolInvocationResult
      {
         ToolInvocationId = request.ToolInvocationId,
         ToolPluginId = toolId,
         ToolVersion = "UNKNOWN", // See architecture note below
         CapabilityName = request.CapabilityName,
         TaskId = request.TaskId,
         Outcome = "error",
         ExitCode = -1,
         DurationMs = durationMs,
         StartedAt = startedAt,
         CompletedAt = DateTimeOffset.UtcNow,
         NextAction = "escalate", // Route execution exception directly to Governance
         NormalizedBy = "ToolGatewayService"
      };
   }
}
