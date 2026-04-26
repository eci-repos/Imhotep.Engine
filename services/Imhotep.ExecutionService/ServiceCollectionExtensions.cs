using Microsoft.Extensions.DependencyInjection;
using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Implementation;
using Imhotep.Orchestration.Services;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.ModelGateway.Providers;

public static class ImhotepServiceCollectionExtensions
{
   public static IServiceCollection AddImhotepExecutionRuntime(this IServiceCollection services)
   {
      // 1. Register the Model Abstraction Layer
      // This ensures the Orchestrator and Agents can invoke the Semantic Kernel
      services.AddSingleton<IModelGateway, SemanticKernelModelGateway>();

      // 2. Wire the Agents into the Roster
      // By registering as IAgent, the DI container automatically supplies this to IEnumerable<IAgent>
      services.AddTransient<IAgent, ImplementationGenerator>();

      // As you build more agents (like the RepairAnalyst), you simply add them to the roster here:
      // services.AddTransient<IAgent, RepairAnalyst>();
      // services.AddTransient<IAgent, ArchitecturePlanner>();

      // 3. Register the Agent Orchestrator
      services.AddTransient<IAgentOrchestrator, AgentOrchestrator>();

      return services;
   }
}
