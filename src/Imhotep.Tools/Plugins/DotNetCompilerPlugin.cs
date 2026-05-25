using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Tools.Abstractions;
using Imhotep.Common.Models;
using Imhotep.Tools.Gateway;

namespace Imhotep.Tools.Plugins;

public class DotNetCompilerPlugin : IToolPlugin
{
   private readonly ILogger<DotNetCompilerPlugin> _logger;
   public string ToolId => "TOOL-DOTNET-CLI-01";
   public string CapabilityName => "compile";

   // ISL v3.9 Sec 18.1: Trust Profile Enforcement
   public IReadOnlyList<string> AllowedEnvironments => new List<string> { "local", "dev-container" };

   public DotNetCompilerPlugin(ILogger<DotNetCompilerPlugin> logger)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
   }

   // The missing method implemented!
   public bool IsAuthorizedForEnvironment(string environmentProfileId)
   {
      if (string.IsNullOrWhiteSpace(environmentProfileId)) return false;

      // ISL v3.9 Sec 18.2: A restricted tool MUST be used only within its declared constraints.
      return AllowedEnvironments.Contains(environmentProfileId, StringComparer.OrdinalIgnoreCase);
   }

   public async Task<ToolInvocationResult> ExecuteAsync(ToolInvocationRequest request, CancellationToken cancellationToken = default)
   {
      var startTime = DateTimeOffset.UtcNow;
      var stopwatch = Stopwatch.StartNew();
      var findings = new List<ToolFinding>();

      // 1. Pre-generate the Result ID so findings can explicitly link to it [ISL v1.6 Sec 13.0]
      string invocationResultId = $"VRS-{Guid.NewGuid():N}";

      // 2. Establish an Isolated Execution Boundary (Sandbox) [ISL v3.9 Sec 17.0]
      string sandboxDirectory = Path.Combine(Path.GetTempPath(), "Imhotep_Sandbox", Guid.NewGuid().ToString());
      Directory.CreateDirectory(sandboxDirectory);

      try
      {
         // 3. Hydrate the sandbox 
         // In a full implementation, you would resolve request.ArtifactIds from the IArtifactRepository here.
         // Assuming you handle the hydration logic based on the payload:
         await HydrateSandboxAsync(sandboxDirectory, request, cancellationToken);

         // 3. Execute the deterministic tool (.NET CLI)
         var processInfo = new ProcessStartInfo
         {
            FileName = "dotnet",
            Arguments = $"build \"{sandboxDirectory}\" -c Release /nologo",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = sandboxDirectory
         };

         using var process = new Process { StartInfo = processInfo };
         process.Start();

         // Respect the Execution Runtime's CancellationToken
         string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
         string errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
         await process.WaitForExitAsync(cancellationToken);

         bool isSuccess = process.ExitCode == 0;

         // 4. Enforce Structured Output Contract & Normalization [ISL v1.6 Sec 13.0]
         if (!isSuccess)
         {
            var compilerErrors = ExtractCompilerErrors(output);

            foreach (var err in compilerErrors)
            {
               findings.Add(new ToolFinding
               {
                  ToolInvocationResultId = invocationResultId, // Linked to 
                  Severity = "high", // Compilation failures are High/Critical blockers
                  FindingCategory = "compile",
                  Message = err,
                  RecommendedAction = "repair",
                  BlocksProgression = true
               });
            }

            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
               findings.Add(new ToolFinding
               {
                  ToolInvocationResultId = invocationResultId, // Linked to 
                  Severity = "error",
                  FindingCategory = "tool-execution",
                  Message = $"Process Error: {errorOutput.Trim()}",
                  RecommendedAction = "escalate",
                  BlocksProgression = true
               });
            }
         }

         stopwatch.Stop();

         // 5. Return the Normalized ToolInvocationResult [ISL v1.6 Sec 12.1]
         return new ToolInvocationResult
         {
            TaskId = request.TaskId,
            ToolVersion = request.ToolVersion,
            ToolInvocationResultId = invocationResultId,
            ToolInvocationId = request.ToolInvocationId,
            ToolPluginId = ToolId,
            CapabilityName = CapabilityName,
            Outcome = isSuccess ? "passed" : "failed", // Normalized enums
            Findings = findings.Count > 0 ? findings : null,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow,
            NextAction = isSuccess ? "proceed" : "repair"
         };
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Tool execution error in sandbox {SandboxDir}", sandboxDirectory);
         stopwatch.Stop();

         return new ToolInvocationResult
         {
            TaskId = request.TaskId,
            ToolVersion = request.ToolVersion,
            ToolInvocationResultId = $"VRS-{Guid.NewGuid():N}",
            ToolInvocationId = request.ToolInvocationId,
            ToolPluginId = ToolId,
            CapabilityName = CapabilityName,
            Outcome = "error", // ISL v1.6 Sec 12.1 dictates catastrophic failures log as 'error'
            Findings = new List<ToolFinding>
                    {
                        new ToolFinding
                        {
                            ToolInvocationResultId = invocationResultId, // Linked to 
                            Severity = "critical",
                            FindingCategory = "tool-error",
                            Message = ex.Message,
                            RecommendedAction = "escalate",
                            BlocksProgression = true
                        }
                    },
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow,
            NextAction = "escalate"
         };
      }
      finally
      {
         // 6. Sandbox Teardown
         if (Directory.Exists(sandboxDirectory))
         {
            Directory.Delete(sandboxDirectory, recursive: true);
         }
      }
   }

   private Task HydrateSandboxAsync(string sandboxDirectory, ToolInvocationRequest request, CancellationToken cancellationToken)
   {
      // Placeholder: Fetch physical files from the Artifact Repository utilizing request.ArtifactIds
      // and write them to the sandboxDirectory just like your original code did.
      return Task.CompletedTask;
   }

   /// <summary>
   /// Translates unstructured compiler logs into discrete error findings.
   /// </summary>
   private IEnumerable<string> ExtractCompilerErrors(string rawConsoleOutput)
   {
      var parsedErrors = new List<string>();
      var lines = rawConsoleOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

      foreach (var line in lines)
      {
         // Extract formal C# errors (e.g., "Program.cs(12,5): error CS1002: ; expected")
         if (line.Contains("error CS"))
         {
            parsedErrors.Add(line.Trim());
         }
      }

      // Fallback if compilation failed but no specific CS errors were parsed
      if (!parsedErrors.Any() && !string.IsNullOrWhiteSpace(rawConsoleOutput))
      {
         parsedErrors.Add("Compilation failed: Check dependencies or project structure.");
      }

      return parsedErrors;
   }
}
