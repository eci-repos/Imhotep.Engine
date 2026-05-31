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
      Task<ToolInvocationResult> ExecuteToolAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default);
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

}
