using Imhotep.Agents.Abstractions;
using Imhotep.Governance.Services;
using Imhotep.Observability.Services;
using Imhotep.Orchestration.Services;
using Imhotep.Planning.Services;
using Imhotep.Repository.Configuration;
using Imhotep.Repository.Services;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;
using Imhotep.Specification.Intake;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Parsing;
using Imhotep.TelemetryService.Monitoring;
using Imhotep.Specification.Pipeline;
using Imhotep.State.Abstractions;
using Imhotep.State.Stores;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
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
         // 1. Bind the intake options strictly from appsettings.json (The Enterprise Way)
         services.Configure<LocalIntakeOptions>(hostContext.Configuration.GetSection("LocalIntake"));

         // 2. Register the implementation against the abstraction
         services.AddSingleton<ISpecificationIntake, LocalFileSystemSpecificationIntake>();

         // 3. Register Phase 1-4 Internal Components
         services.AddSingleton<IPayloadParser, MarkdownSTPParser>();
         services.AddSingleton<ISemanticNormalizer, SemanticNormalizer>();
         services.AddSingleton<IReadinessEvaluator, ReadinessEvaluator>();
         services.AddSingleton<IClarificationFormatter, ClarificationFormatter>();
         services.AddSingleton<IResponseDispatcher, ResponseDispatcher>();

         // 4. Register the Encapsulated Intake Pipeline
         services.AddSingleton<Specification.Pipeline.SpecificationIntakePipeline>();

         // 5. Register Day-2 Orchestration Subsystems
         services.Configure<ArtifactRepositoryOptions>(hostContext.Configuration.GetSection("ArtifactRepository"));
         services.AddSingleton<IArtifactRepository, ArtifactRepository>();
         services.AddSingleton<IPlanningEngine, PlanningEngine>();

         // Register the Generic State Store (ISL v2.2) 
         services.AddSingleton(typeof(ILogicalStateStore<>), typeof(InMemoryLogicalStateStore<>));
         services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();

         // ISL v1.7 Governance & Control Model
         // Registers the enterprise governance engine we built to handle approval gates, waivers, and escalations.
         services.AddSingleton<IGovernanceService, Imhotep.Governance.Services.GovernanceService>();

         // ISL v2.6 Observability & Telemetry Model
         // Registers our upgraded internal concurrent telemetry store
         services.AddSingleton<ITelemetryService, Imhotep.Observability.Services.TelemetryService>();

         // Registers the OpenTelemetry Adapter we just built (Provider-specific logic)
         services.AddSingleton<Imhotep.Adapters.Telemetry.OpenTelemetryExporterAdapter>();

         // Native .NET OpenTelemetry Integration
         services.AddOpenTelemetry()
             .WithTracing(tracing => tracing
                 .AddSource("Imhotep.ExecutionRuntime") // Must match the ActivitySource in your adapter
                 .AddOtlpExporter()); // Exports to external dashboards like Jaeger or Datadog

         // ISL v2.6 The Watchtower
         // Registers our background worker that constantly scans the TelemetryService for tool failures
         services.AddHostedService<EnterpriseWatchtowerAlertEngine>();

         // 6. Initialize Microsoft Semantic Kernel (The Cognitive Engine)
         ConfigureSemanticKernel(hostContext, services);

         // 7. Register the Main Execution Loop
         services.AddHostedService<ConstructionRuntimeWorker>();
      });


}
