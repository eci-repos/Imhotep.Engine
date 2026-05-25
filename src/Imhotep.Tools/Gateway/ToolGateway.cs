using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Imhotep.Tools.Gateway
{
   /// <summary>
   /// Acts as the boundary between autonomous construction and deterministic engineering systems,
   /// routing requests to tool plugins and normalizing results (ISL v1.6 Section 6.0).
   /// </summary>
   public interface IToolGateway
   {
      /// <summary>
      /// ISL v1.6: Invokes a deterministic tool and returns normalized, structured results.
      /// </summary>
      Task<ToolInvocationResult> InvokeToolAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default);
   }
   /// <summary>
   /// ISL v3.9 Sec 7.0 & 18.0: The standard interface for all deterministic tool plugins.
   /// </summary>
   public interface IToolPlugin
   {
      public string ToolId { get; }

      /// <summary>
      /// ISL v3.9 Sec 8.0: Capability Declaration (e.g., "compile", "security-scan")
      /// </summary>
      public string CapabilityName { get; }

      /// <summary>
      /// ISL v3.9 Sec 18.1: Trust Profile Enforcement
      /// The list of environments where this tool is legally permitted to execute.
      /// </summary>
      public IReadOnlyList<string> AllowedEnvironments { get; }

      /// <summary>
      /// Evaluates if the tool's Trust Profile authorizes it to run in the requested environment.
      /// </summary>
      public bool IsAuthorizedForEnvironment(string environmentProfileId);

      /// <summary>
      /// Executes the deterministic tool and returns structured, normalized findings.
      /// </summary>
      public Task<ToolInvocationResult> ExecuteAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default);
   }

   /// <summary>
   /// The core abstraction layer routing requests to the correct deterministic tool plugin (ISL v3.9).
   /// </summary>
   public class ToolGateway : IToolGateway
   {
      private readonly IEnumerable<IToolPlugin> _plugins;
      private readonly ILogger<ToolGateway> _logger;

      public ToolGateway(IEnumerable<IToolPlugin> plugins, ILogger<ToolGateway> logger)
      {
         _plugins = plugins;
         _logger = logger;
      }

      public async Task<ToolInvocationResult> InvokeToolAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         // 1. Emits standard telemetry [ISL v1.6 Sec 26.1]
         _logger.LogInformation("ToolGateway routing invocation for task {TaskId} using capability {Capability}",
             request.TaskId, request.CapabilityName); // Fixed property name

         // 2. Tool Selection: Match Capability AND Artifact Type Compatibility [ISL v1.6 Sec 10.1 & 7.3]
         var targetPlugin = _plugins.FirstOrDefault(p =>
             p.CapabilityName.Equals(request.CapabilityName, StringComparison.OrdinalIgnoreCase) &&
             p.IsAuthorizedForEnvironment(request.EnvironmentProfileId));
          
         if (targetPlugin == null)
         {
            _logger.LogError("No registered or authorized tool plugin found for capability {Capability}.", request.CapabilityName);

            // 3. Strict Normalization to "error" [ISL v1.6 Sec 12.2]
            return CreateNormalizedResult(
                request,
                toolId: request.ToolPluginId,
                errorMessage: $"System Error: Unresolvable or unauthorized tool capability '{request.CapabilityName}'");
         }

         try
         {
            // Execute the deterministic tool via the plugin in its isolated boundary
            var result = await targetPlugin.ExecuteAsync(request, cancellationToken);

            _logger.LogInformation("Tool Invocation Completed. Outcome: {Outcome}", result.Outcome);
            return result;
         }
         catch (Exception ex)
         {
            // 4. Catastrophic Isolation Failure Handling [ISL v1.6 Sec 15.5 & 31.2]
            _logger.LogError(ex, "Catastrophic failure during tool plugin execution for capability {Capability}.", request.CapabilityName);

            return CreateNormalizedResult(
                request,
                toolId: request.ToolPluginId,
                errorMessage: $"Tool Execution Exception in {targetPlugin.GetType().Name}: {ex.Message}");
         }
      }

      /// <summary>
      /// Normalizes gateway failures into strict ISL v3.9 ToolInvocationResult contracts.
      /// </summary>
      private ToolInvocationResult CreateNormalizedResult(ToolInvocationRequest request, string toolId, string errorMessage)
      {
         // 1. Generate the ID upfront so we can mathematically link the ToolFinding to the parent result
         var resultId = $"VRS-{Guid.NewGuid():N}";
         var now = DateTimeOffset.UtcNow;

         return new ToolInvocationResult
         {
            ToolInvocationResultId = resultId, // Overrides the default to ensure exact linkage
            ToolInvocationId = request.ToolInvocationId ?? $"TINV-{Guid.NewGuid():N}",
            ToolPluginId = toolId,
            ToolVersion = "Unknown", // Replaces legacy ToolVersionConfirmed
            CapabilityName = request.CapabilityName,
            TaskId = request.TaskId,

            // ISL v3.9 explicit artifact tracking properties
            AffectedArtifactIds = new List<string>().AsReadOnly(),
            EvaluatedArtifactVersionIds = new List<string>().AsReadOnly(),

            Outcome = "error",
            ExitCode = -1, // Captures process boundary failure [ISL v3.9]

            DurationMs = 0,
            StartedAt = now,
            CompletedAt = now,

            NormalizedBy = "ToolGateway",
            NextAction = "escalate",

            Findings = new List<ToolFinding>
            {
                new ToolFinding
                {
                    ToolFindingId = $"FND-{Guid.NewGuid():N}",
                    ToolInvocationResultId = resultId, // Formal traceability link
                    Severity = "critical",
                    FindingCategory = "infrastructure",
                    Message = errorMessage,
                    RecommendedAction = "escalate",
                    BlocksProgression = true
                }
            }.AsReadOnly()
         };
      }

   }
}
