

using System.Collections.Generic;
using System.Threading.Tasks;
using Imhotep.Tools.Abstractions;
using Imhotep.Common.Models;

namespace Imhotep.ToolGateway.Plugins;

/// <summary>
/// A deterministic plugin adapter that validates JSON/Object structures 
/// against formal domain schemas (e.g., NCSC NODS canonical schemas).
/// </summary>
public class SchemaValidationPlugin : IValidationPlugin
{
   public string CapabilityName => "SchemaValidation";

   public Task<ValidationResult> ExecuteValidationAsync(ValidationRequest request)
   {
      // 1. Extract the raw JSON string from the dictionary. 
      // If the agent generated a single file, we grab the first value.
      string jsonContentToValidate = request.ArtifactContent.Values.FirstOrDefault() ?? string.Empty;

      // 2. Isolate and Execute External Tool
      // Now we pass the extracted string, resolving the compiler type mismatch.
      bool rawToolPassed = SimulateExternalSchemaCheck(jsonContentToValidate);

      // 3. Map to standard ValidationResult
      var result = new ValidationResult
      {
         IsSuccessful = rawToolPassed,
         ValidationRuleId = request.ValidationRuleId,
         Errors = rawToolPassed ? new List<string>() : new List<string> { "Schema validation failed against NCSC NODS structure." },
         SecurityFindings = new List<string>()
      };

      return Task.FromResult(result);
   }

   private bool SimulateExternalSchemaCheck(string artifactContent)
   {
      // Deterministic external execution logic goes here
      return !string.IsNullOrWhiteSpace(artifactContent);
   }
}

