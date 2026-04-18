
using Imhotep.Tools.Models;
using System.Threading.Tasks;

namespace Imhotep.Tools.Services;

/// <summary>
/// The standard adapter interface for all external deterministic engineering tools.
/// </summary>
public interface IValidationPlugin
{
   /// <summary>
   /// Declares the capability of the tool (e.g., "SchemaValidation", "StaticSecurityAnalysis", "PIIScanner").
   /// </summary>
   string CapabilityName { get; }

   /// <summary>
   /// Executes the external tool in an isolated boundary and returns a strictly formatted result.
   /// </summary>
   Task<ValidationResult> ExecuteValidationAsync(ValidationRequest request);
}
