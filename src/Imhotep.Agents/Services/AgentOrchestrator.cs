using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.Planning.Models;
using Microsoft.Extensions.Logging;

namespace Imhotep.Orchestration.Services
{

   /// <summary>
   /// Coordinates reasoning components and manages communication between specialized agents (ISL v2.0).
   /// </summary>
   public class AgentOrchestrator : IAgentOrchestrator
   {
      private readonly IModelGateway _modelGateway;
      private readonly IEnumerable<IAgent> _availableAgents;
      private readonly ILogger<AgentOrchestrator> _logger;

      public AgentOrchestrator(
          IModelGateway modelGateway,
          IEnumerable<IAgent> availableAgents,
          ILogger<AgentOrchestrator> logger)
      {
         _modelGateway = modelGateway;
         _availableAgents = availableAgents;
         _logger = logger;
      }

      public async Task<AgentResult> DispatchTaskAsync(ConstructionTask task, AgentContext context, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Agent Orchestrator evaluating Task {TaskId} ({Category}).", task.TaskId, task.Category);

         // 1. Capability-Based Routing: Determine the appropriate agent role (ISL v2.1)
         IAgent targetAgent = ResolveAgentForTask(task);

         if (targetAgent == null)
         {
            _logger.LogError("No suitable agent found in the roster for Task Category {Category}.", task.Category);
            throw new InvalidOperationException($"Unresolvable agent assignment for category {task.Category}");
         }

         _logger.LogInformation("Task {TaskId} assigned to the {RoleName} agent.", task.TaskId, targetAgent.RoleName);

         try
         {
            // 2. Agent Invocation Pipeline (ISL v2.4): Execute the bounded reasoning transaction
            AgentResult result = await targetAgent.ExecuteTaskAsync(task, context, _modelGateway, cancellationToken);

            // 3. Evaluate Output Contract
            if (!result.IsSuccess)
            {
               _logger.LogWarning("{RoleName} failed to complete Task {TaskId}. Reason: {Error}", targetAgent.RoleName, task.TaskId, result.ErrorMessage);
               // Note: The Execution Runtime will catch this failure and potentially escalate to a Repair Cycle.
            }
            else
            {
               _logger.LogInformation("{RoleName} successfully completed Task {TaskId} and generated {Count} artifacts.",
                   targetAgent.RoleName, task.TaskId, result.GeneratedArtifacts.Count);
            }

            return result;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Systemic failure during agent execution for Task {TaskId}.", task.TaskId);
            throw;
         }
      }

      /// <summary>
      /// Maps the required construction category directly to the corresponding cognitive agent.
      /// </summary>
      private IAgent ResolveAgentForTask(ConstructionTask task)
      {
         // If the task failed deterministic validation previously, it is routed to the Repair Analyst
         if (task.ExecutionStatus == PlanTaskStatus.InRepair)
         {
            return _availableAgents!.FirstOrDefault(a => a.RoleName == "Repair Analyst");
         }

         // Normal capability-based selection [9, 10]
         return task.Category switch
         {
            TaskCategory.Architecture => _availableAgents!.FirstOrDefault(a => a.RoleName == "Architecture Planner"),
            TaskCategory.Implementation => _availableAgents!.FirstOrDefault(a => a.RoleName == "Implementation Generator"),
            TaskCategory.Verification => _availableAgents!.FirstOrDefault(a => a.RoleName == "Test Generator"),
            TaskCategory.Infrastructure => _availableAgents!.FirstOrDefault(a => a.RoleName == "Deployment Preparer"),
            _ => null
         };
      }
   }
}
