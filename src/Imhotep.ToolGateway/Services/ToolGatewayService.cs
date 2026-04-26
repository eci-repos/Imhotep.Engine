
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Common.Models;
using Imhotep.ToolGateway.Core;
using Imhotep.Tools.Abstractions;

namespace Imhotep.ToolGateway.Services;

/// <summary>
/// The standalone service routing deterministic validation requests from the 
/// Execution Runtime to the appropriate isolated tool plugins.
/// </summary>
public class ToolGatewayService : IValidationPlugin
{
   private readonly IToolRegistry _toolRegistry;
   private readonly ILogger<ToolGatewayService> _logger;

   // The gateway itself exposes the same interface, acting as the master router.
   public string CapabilityName => "ToolGatewayRouter";

   public ToolGatewayService(IToolRegistry toolRegistry, ILogger<ToolGatewayService> logger)
   {
      _toolRegistry = toolRegistry;
      _logger = logger;
   }

   public async Task<ValidationResult> ExecuteValidationAsync(ValidationRequest request)
   {
      _logger.LogInformation("Routing validation request for Rule: {ValidationRuleId}, Target: {TargetTraceabilityId}",
          request.ValidationRuleId, request.TargetTraceabilityId);

      try
      {
         // 1. Tool Discovery
         var plugin = _toolRegistry.GetPluginForRule(request.ValidationRuleId);

         if (plugin == null)
         {
            return new ValidationResult
            {
               IsSuccessful = false,
               ValidationRuleId = request.ValidationRuleId,
               Errors = new[] { $"CRITICAL: No registered tool plugin found for rule {request.ValidationRuleId}." }
            };
         }

         // 2. Sandboxed Tool Invocation
         // The plugin handles the execution inside an isolated environment (e.g., a container).
         var result = await plugin.ExecuteValidationAsync(request);

         // 3. Telemetry and Result Routing
         _logger.LogInformation("Validation completed for {ValidationRuleId}. Success: {IsSuccessful}",
             request.ValidationRuleId, result.IsSuccessful);

         return result;
      }
      catch (Exception ex)
      {
         // Ensure internal tool crashes do not bring down the IMHOTEP runtime
         _logger.LogError(ex, "Tool execution boundary violation during {ValidationRuleId}.", request.ValidationRuleId);

         return new ValidationResult
         {
            IsSuccessful = false,
            ValidationRuleId = request.ValidationRuleId,
            Errors = new[] { $"Tool execution boundary violation: {ex.Message}" }
         };
      }
   }
}

