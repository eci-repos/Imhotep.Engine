
using System.Collections.Generic;
using Imhotep.Tools.Abstractions;

namespace Imhotep.ToolGateway.Core;

/// <summary>
/// Maintains the catalog of available deterministic engineering tools.
/// Enforces the Tool Discovery and Registration phase of the plugin lifecycle.
/// </summary>
public interface IToolRegistry
{
   /// <summary>
   /// Registers a new deterministic tool adapter into the platform environment.
   /// </summary>
   void RegisterPlugin(IValidationPlugin plugin);

   /// <summary>
   /// Retrieves a specific tool plugin based on the required validation rule 
   /// (e.g., retrieving the "SchemaValidationPlugin" for VAL-001).
   /// </summary>
   IValidationPlugin GetPluginForRule(string validationRuleId);

   /// <summary>
   /// Returns all registered plugins and their declared capabilities.
   /// </summary>
   IReadOnlyList<string> GetAvailableCapabilities();
}

