using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Imhotep.Tools.Abstractions;
using Imhotep.Common.Models;

namespace Imhotep.Tools.Plugins;

public class DotNetCompilerPlugin : IValidationPlugin
{
   public string CapabilityName => "Compile.DotNet";

   public async Task<ValidationResult> ExecuteValidationAsync(ValidationRequest request)
   {
      // 1. Establish an Isolated Execution Boundary (Sandbox)
      string sandboxDirectory = Path.Combine(Path.GetTempPath(), "Imhotep_Sandbox", Guid.NewGuid().ToString());
      Directory.CreateDirectory(sandboxDirectory);

      var errors = new List<string>();

      try
      {
         // 2. Hydrate the sandbox with the ArtifactContent
         foreach (var artifact in request.ArtifactContent)
         {
            string filePath = Path.Combine(sandboxDirectory, artifact.Key);
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
               Directory.CreateDirectory(directoryPath);

            await File.WriteAllTextAsync(filePath, artifact.Value);
         }

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

         string output = await process.StandardOutput.ReadToEndAsync();
         string errorOutput = await process.StandardError.ReadToEndAsync();
         await process.WaitForExitAsync();

         bool isSuccess = process.ExitCode == 0;

         // 4. Enforce Structured Output Contract
         if (!isSuccess)
         {
            errors.AddRange(ExtractCompilerErrors(output));
            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
               errors.Add($"Process Error: {errorOutput.Trim()}");
            }
         }

         return new ValidationResult
         {
            IsSuccessful = isSuccess,
            ValidationRuleId = request.ValidationRuleId,
            Errors = errors,
            SecurityFindings = new List<string>() // Addressed by a separate security plugin
         };
      }
      finally
      {
         // 5. Sandbox Teardown
         if (Directory.Exists(sandboxDirectory))
         {
            Directory.Delete(sandboxDirectory, recursive: true);
         }
      }
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
