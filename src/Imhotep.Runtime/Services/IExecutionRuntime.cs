using Imhotep.Planning.Models;
using Imhotep.Runtime.Models;
using Imhotep.SemanticModel;
using Imhotep.SemanticModel.Graph;
using System.Threading.Tasks;

namespace Imhotep.Runtime.Services;

/// <summary>
/// The central operational engine responsible for executing the autonomous construction workflow.
/// Acts as the "construction foreman" coordinating tasks, agents, tools, and repair loops.
/// </summary>
public interface IExecutionRuntime
{
   /// <summary>
   /// Executes the complete sequence of construction activities defined in the task graph.
   /// Coordinates agent generation, deterministic tool validation, and iterative repair cycles 
   /// until the system reaches a stable state that satisfies the specification.
   /// </summary>
   Task<ExecutionState> ExecuteConstructionPlanAsync(
       ConstructionTaskGraph taskGraph,
       CanonicalSemanticModel semanticModel,
       CancellationToken stoppingToken = default);

   /// <summary>
   /// Retrieves the current persistent execution state to support observability, 
   /// telemetry, and workflow resumption.
   /// </summary>
   Task<ExecutionState> GetCurrentStateAsync(string transactionId);
}
