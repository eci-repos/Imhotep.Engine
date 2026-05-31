
using Imhotep.Tools.Abstractions;
using Imhotep.Tools.Gateway;
using System.Collections.Generic;

namespace Imhotep.ToolGateway.Models;

/// <summary>
/// ISL v3.9 Sec 9.0: The Tool Registry.
/// Maintains the catalog of available deterministic engineering tools.
/// Enforces the Tool Discovery and Registration phase of the plugin lifecycle.
/// </summary>
public interface IToolRegistry
{
   /// <summary>
   /// Registers a new deterministic tool adapter into the platform environment.
   /// </summary>
   void RegisterPlugin(IToolPlugin plugin);

   /// <summary>
   /// ISL v3.9 Sec 12.0: Tool Selection.
   /// Retrieves a specific tool plugin based on the required capability 
   /// (e.g., retrieving a plugin that satisfies "schema-validation" or "compile").
   /// </summary>
   IToolPlugin? GetPluginForCapability(string capabilityName);

   /// <summary>
   /// Returns all registered plugins and their declared capabilities.
   /// </summary>
   IReadOnlyList<string> GetAvailableCapabilities();
}

