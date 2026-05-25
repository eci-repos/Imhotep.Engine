using System.Threading.Tasks;
using Imhotep.Agents.Models;
using Imhotep.Agents.Abstractions;
using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Agents.Abstractions;

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
   Task<AgentResult> ExecuteTaskAsync(AgentContextPackage context);
}

/// <summary>
/// Coordinates the reasoning agents, assigns tasks, and integrates with the underlying 
/// AI models (via the Model Integration Layer / Semantic Kernel).
/// </summary>
public interface IAgentOrchestrator
{
   Task<AgentContextPackage> AssembleContextAsync(
       ConstructionTask task,
       string agentRole,
       CancellationToken cancellationToken = default);

   /// <summary>
   /// ISL v2.1 Sec 8.0: Agent Invocation Contract.
   /// Invokes the agent using the strict context package.
   /// </summary>
   Task<AgentOutputRecord> InvokeAgentAsync(
       ConstructionTask task,
       AgentContextPackage contextPackage,
       CancellationToken cancellationToken = default);
}
