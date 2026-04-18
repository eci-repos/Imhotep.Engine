using System.Threading.Tasks;
using Imhotep.Agents.Models;
using Imhotep.Planning.Services;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Agents.Services;

/// <summary>
/// Defines the contract for specialized reasoning components 
/// (e.g., "Implementation Generator", "Repair Analyst").
/// </summary>
public interface IReasoningAgent
{
   /// <summary>
   /// The specific cognitive role this agent fulfills within the construction lifecycle.
   /// </summary>
   string RoleName { get; }

   /// <summary>
   /// Executes the reasoning task based on the provided context, returning a structured response.
   /// </summary>
   Task<AgentResponse> ExecuteTaskAsync(AgentContext context);
}

/// <summary>
/// Coordinates the reasoning agents, assigns tasks, and integrates with the underlying 
/// AI models (via the Model Integration Layer / Semantic Kernel).
/// </summary>
public interface IAgentOrchestrator
{
   /// <summary>
   /// Analyzes a construction task and dispatches it to the appropriate specialized agent.
   /// </summary>
   Task<AgentResponse> DispatchTaskAsync(ConstructionTask task, CanonicalSemanticModel semanticModel);
}
