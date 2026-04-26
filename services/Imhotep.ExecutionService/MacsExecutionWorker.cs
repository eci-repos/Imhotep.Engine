using Imhotep.Planning.Services;
using Imhotep.Repository.Services;
using Imhotep.Runtime.Services;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Parsing;
using Imhotep.SemanticModel.Graph;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.ExecutionService
{
   public class MacsExecutionWorker : BackgroundService
   {
      private readonly IPayloadParser _stpParser;
      private readonly ISemanticNormalizer _semanticNormalizer;
      private readonly IReadinessEvaluator _readinessEvaluator;
      private readonly IArtifactRepository _artifactRepository;
      private readonly IPlanningEngine _planningEngine;
      private readonly IExecutionRuntime _executionRuntime;
      private readonly ILogger<MacsExecutionWorker> _logger;

      public MacsExecutionWorker(
          IPayloadParser stpParser,
          ISemanticNormalizer semanticNormalizer,
          IReadinessEvaluator readinessEvaluator,
          IArtifactRepository artifactRepository,
          IPlanningEngine planningEngine,
          IExecutionRuntime executionRuntime,
          ILogger<MacsExecutionWorker> logger)
      {
         _stpParser = stpParser;
         _semanticNormalizer = semanticNormalizer;
         _readinessEvaluator = readinessEvaluator;
         _artifactRepository = artifactRepository;
         _planningEngine = planningEngine;
         _executionRuntime = executionRuntime;
         _logger = logger;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
      {
         _logger.LogInformation("IMHOTEP MACS Execution Service starting...");

         try
         {
            // 1. Specification Intake: Read the approved STP
            string stpPath = Path.Combine(AppContext.BaseDirectory, "payloads", "PROJ-INTAKE-001.md");
            string rawPayload = await File.ReadAllTextAsync(stpPath, stoppingToken);

            // 2. Parse & Normalize: Convert to the Canonical Semantic Model
            var parsedPayload = await _stpParser.ParseAsync(rawPayload);
            var semanticModel = await _semanticNormalizer.NormalizeAsync(parsedPayload, stoppingToken);
            _logger.LogInformation("Successfully normalized Canonical Semantic Model for Transaction {TransactionId}", semanticModel.TransactionId);

            // 3. Evaluate Readiness: Verify it clears Human Approval Gates (ISL v1.3)
            var readinessStatus = await _readinessEvaluator.EvaluateAsync(semanticModel, stoppingToken);

            if (readinessStatus.Level == ReadinessLevel.AutonomousReady)
            {
               // 4. The Mechanical Handoff: Commit Baseline to the Artifact Repository
               await _artifactRepository.CommitChangesAsync(
                   semanticModel.TransactionId,
                   "Baseline Commit: MACS Proof-of-Concept Specification Authorized for Execution");

               _logger.LogInformation("Specification is Autonomous-Ready. Passing control to the Planning Engine.");

               // 5. Construction Planning: Generate the Task Graph
               var taskGraph = await _planningEngine.GenerateTaskGraphAsync(semanticModel, stoppingToken);

               // 6. Autonomous Execution: Dispatch Agents and Tools
               await _executionRuntime.ExecuteConstructionPlanAsync(taskGraph, semanticModel, stoppingToken);
            }
            else
            {
               // 7. Human-Machine Escalation ("Andon Cord"): Halt if not authorized
               _logger.LogError("Execution Halted. Specification is not Autonomous-Ready. Current Level: {Level}. Outstanding Exceptions: {Exceptions}",
                   readinessStatus.Level, string.Join(", ", readinessStatus.Exceptions));
            }
         }
         catch (Exception ex)
         {
            _logger.LogCritical(ex, "A fatal structural error occurred during the MACS Execution Loop.");
         }
      }

   }
}
