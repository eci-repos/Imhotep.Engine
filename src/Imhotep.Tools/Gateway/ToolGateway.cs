using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Tools.Abstractions;
using Imhotep.Common.Models;

namespace Imhotep.Tools.Gateway
{
   public interface IToolGateway
   {
      Task<ValidationResult> InvokeValidationAsync(string targetCapability, ValidationRequest request);
   }

   /// <summary>
   /// The core abstraction layer routing requests to the correct deterministic tool plugin (ISL v3.9).
   /// </summary>
   public class ToolGateway : IToolGateway
   {
      private readonly IEnumerable<IValidationPlugin> _plugins;
      private readonly ILogger<ToolGateway> _logger;

      public ToolGateway(IEnumerable<IValidationPlugin> plugins, ILogger<ToolGateway> logger)
      {
         _plugins = plugins;
         _logger = logger;
      }

      public async Task<ValidationResult> InvokeValidationAsync(string targetCapability, ValidationRequest request)
      {
         _logger.LogInformation("ToolGateway routing validation rule {RuleId} for artifact {TraceabilityId} using capability {Capability}",
             request.ValidationRuleId, request.TargetTraceabilityId, targetCapability);

         // Dynamically discover the registered plugin (e.g., "Compile.DotNet")
         var targetPlugin = _plugins.FirstOrDefault(p => p.CapabilityName.Equals(targetCapability, StringComparison.OrdinalIgnoreCase));

         if (targetPlugin == null)
         {
            _logger.LogError("No registered plugin found for capability {Capability}.", targetCapability);
            return new ValidationResult
            {
               IsSuccessful = false,
               ValidationRuleId = request.ValidationRuleId,
               Errors = new List<string> { $"System Error: Unresolvable tool capability '{targetCapability}'" }
            };
         }

         try
         {
            // Execute the deterministic tool in its isolated boundary
            return await targetPlugin.ExecuteValidationAsync(request);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Catastrophic failure during tool execution for rule {RuleId}.", request.ValidationRuleId);
            return new ValidationResult
            {
               IsSuccessful = false,
               ValidationRuleId = request.ValidationRuleId,
               Errors = new List<string> { $"Tool Execution Exception: {ex.Message}" }
            };
         }
      }
   }
}
