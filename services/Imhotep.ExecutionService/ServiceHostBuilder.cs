using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;
using Imhotep.Specification.Intake;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Parsing;
using Imhotep.Specification.Pipeline;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Imhotep.ExecutionService;

public class ServiceHostBuilder
{

   public static void ConfigureSemanticKernel(HostBuilderContext hostContext, IServiceCollection services)
   {
      // Extract the configuration from the host context
      var config = hostContext.Configuration;

      var kernelBuilder = Kernel.CreateBuilder();
      var activeProvider = config["AIProviders:ActiveProvider"];

      // Dynamically inject the active provider adapter based on configuration
      if (activeProvider == "OpenAI")
      {
         kernelBuilder.AddOpenAIChatCompletion(
             modelId: config["AIProviders:OpenAI:ModelId"]!,
             apiKey: config["AIProviders:OpenAI:ApiKey"]!);
      }
      else if (activeProvider == "AzureOpenAI")
      {
         kernelBuilder.AddAzureOpenAIChatCompletion(
             deploymentName: config["AIProviders:AzureOpenAI:DeploymentName"]!,
             endpoint: config["AIProviders:AzureOpenAI:Endpoint"]!,
             apiKey: config["AIProviders:AzureOpenAI:ApiKey"]!);
      }
      else if (activeProvider == "LocalModel")
      {
         // Placeholder for local model provider (e.g., Ollama/LM Studio)
         kernelBuilder.AddOpenAIChatCompletion(
            modelId: config["AIProviders:LocalModel:ModelId"]!,
            apiKey: "dummy_api_key",
            endpoint: new Uri(config["AIProviders:LocalModel:EndPoint"]!));
      }

      // Register the constructed Kernel as a Singleton
      services.AddSingleton(kernelBuilder.Build());
   }

   /// <summary>
   /// Configures and creates a new host builder with default settings and application services 
   /// for the application.
   /// </summary>
   /// <remarks>This method sets up dependency injection, configuration, and hosted services 
   /// required for the
   /// application's execution. It should be called from the application's entry point to initialize
   /// the host.</remarks>
   /// <param name="args">An array of command-line arguments to configure the host and application.</param>
   /// <returns>An initialized <see cref="IHostBuilder"/> instance configured with default settings
   /// and registered services.</returns>
   public static IHostBuilder CreateHostBuilder(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureServices((hostContext, services) =>
           {
              // 1. Register Phase 1-4 Internal Components
              services.AddSingleton<IPayloadParser, MarkdownSTPParser>();
              services.AddSingleton<ISemanticNormalizer, SemanticNormalizer>();
              services.AddSingleton<IReadinessEvaluator, ReadinessEvaluator>();
              services.AddSingleton<IClarificationFormatter, ClarificationFormatter>();
              services.AddSingleton<IResponseDispatcher, ResponseDispatcher>();

              // 2. Register the Encapsulated Intake Pipeline
              services.AddSingleton<SpecificationIntakePipeline>();

              // 3. Register Day-2 Orchestration Subsystems (To be implemented)
              // services.AddSingleton<IArtifactRepository, GitArtifactRepository>();
              // services.AddSingleton<IPlanningEngine, PlanningEngine>();
              // services.AddSingleton<IAgentOrchestrator, SemanticKernelOrchestrator>();

              // 4. Initialize Microsoft Semantic Kernel (The Cognitive Engine)
              ConfigureSemanticKernel(hostContext, services);

              // 5. Register the Main Execution Loop
              services.AddHostedService<ConstructionRuntimeWorker>();
           });

}
