using Imhotep.Contracts.Governance;
using Imhotep.Planning.Services;
using Imhotep.Repository.Services;
using Imhotep.Runtime.Services;
using Imhotep.SemanticModel.Graph;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;
using Imhotep.Specification.Intake;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Parsing;
using Imhotep.Specification.Pipeline;
using Imhotep.Specification.Pipeline;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.ExecutionService
{

   public record ParsedAndExecuteResult
   {
      public SpecificationReadinessReport ReadinessReport { get; init; }
      public CanonicalSemanticModel SemanticModel { get; init; }
   }

   public class ConstructionRuntimeWorder : BackgroundService
   {
      private readonly IPayloadParser _stpParser;
      private readonly ISpecificationIntakePipeline _intakePipeline;
      private readonly ISpecificationIntake _specificationIntake;
      private readonly ISemanticNormalizer _semanticNormalizer;
      private readonly IReadinessEvaluator _readinessEvaluator;
      private readonly IArtifactRepository _artifactRepository;
      private readonly IPlanningEngine _planningEngine;
      private readonly IExecutionRuntime _executionRuntime;
      private readonly ILogger<ConstructionRuntimeWorder> _logger;

      public ConstructionRuntimeWorder(
          IPayloadParser stpParser,
          SpecificationIntakePipeline intakePipeline,
          ISpecificationIntake _specificationIntake,
          ISemanticNormalizer semanticNormalizer,
          IReadinessEvaluator readinessEvaluator,
          IArtifactRepository artifactRepository,
          IPlanningEngine planningEngine,
          IExecutionRuntime executionRuntime,
          ILogger<ConstructionRuntimeWorder> logger)
      {
         _stpParser = stpParser;
         _intakePipeline = intakePipeline;
         _specificationIntake = _specificationIntake;
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

            PendingPayloadRecord stpRecord = new PendingPayloadRecord
            (
               Guid.NewGuid().ToString(),
               rawPayload,
               stpPath,
               DateTime.UtcNow
            );

            // 2. Parse & Normalize: Convert to the Canonical Semantic Model
            var parsedAndExecuteResult = await ParseAndExecuteAsync(stpRecord, stoppingToken);

            if (parsedAndExecuteResult.ReadinessReport.Level == ReadinessLevel.AutonomousReady)
            {
               // 4. The Mechanical Handoff: Commit Baseline to the Artifact Repository
               await _artifactRepository.CommitChangesAsync(
                   parsedAndExecuteResult.SemanticModel.TransactionId,
                   "Baseline Commit: MACS Proof-of-Concept Specification Authorized for Execution");

               _logger.LogInformation("Specification is Autonomous-Ready. Passing control to the Planning Engine.");

               // 5. Construction Planning: Generate the Task Graph
               var taskGraph = await _planningEngine.GenerateTaskGraphAsync(
                  parsedAndExecuteResult.SemanticModel, parsedAndExecuteResult.ReadinessReport, stoppingToken);

               // 6. Autonomous Execution: Dispatch Agents and Tools
               await _executionRuntime.ExecuteConstructionPlanAsync(taskGraph, parsedAndExecuteResult.SemanticModel, stoppingToken);
            }
            else
            {
               // 7. Human-Machine Escalation ("Andon Cord"): Halt if not authorized
               _logger.LogError("Execution Halted. Specification is not Autonomous-Ready. Current Level: {Level}. Outstanding Exceptions: {Exceptions}",
                   parsedAndExecuteResult.ReadinessReport.Level, 
                   string.Join(", ", parsedAndExecuteResult.ReadinessReport.Exceptions));
            }
         }
         catch (Exception ex)
         {
            _logger.LogCritical(ex, "A fatal structural error occurred during the MACS Execution Loop.");
         }
      }

      /// <summary>
      /// Encapsulates the execution lifecycle for a single Structured Transaction Payload.
      /// </summary>
      private async Task<ParsedAndExecuteResult> ParseAndExecuteAsync(PendingPayloadRecord stpRecord, CancellationToken stoppingToken)
      {
         SpecificationReadinessReport readinessReport = null;
         CanonicalSemanticModel semanticModel = null;
         try
         {
            _logger.LogInformation("Ingesting payload {TransactionId}", stpRecord.TransactionId);

            // 2. Delegate all Parsing, Normalization, and Readiness Evaluation to the Pipeline
            IntakePipelineResult pipelineResult = await _intakePipeline.ProcessPayloadAsync(stpRecord.RawMarkdown, stoppingToken);

            semanticModel = pipelineResult.SemanticModel;
            readinessReport = pipelineResult.ReadinessReport;

            _logger.LogInformation("Successfully normalized Canonical Semantic Model for Transaction {TransactionId}", semanticModel.TransactionId);

            // 3. The Mechanical Handoff: Commit Baseline to the Artifact Repository
            await _artifactRepository.CommitChangesAsync(
                semanticModel.TransactionId,
                "Baseline Commit: MACS Proof-of-Concept Specification Authorized for Execution");

            _logger.LogInformation("Specification is Autonomous-Ready. Passing control to the Planning Engine.");

            // 4. Construction Planning: Generate the Task Graph
            var taskGraph = await _planningEngine.GenerateTaskGraphAsync(semanticModel, readinessReport, stoppingToken);

            // 5. Autonomous Execution: Dispatch Agents and Tools
            await _executionRuntime.ExecuteConstructionPlanAsync(taskGraph, semanticModel, stoppingToken);

            // 6. Update Intake State (e.g., move the markdown file to the /Admitted folder)
            await _specificationIntake.UpdatePayloadStateAsync(stpRecord.TransactionId, IntakeState.Admitted, stoppingToken);
         }
         catch (HumanMachineEscalationException ex)
         {
            // The Digital "Andon Cord": Halts execution strictly for THIS payload.
            _logger.LogError(ex, "Execution Halted for Payload {TransactionId}. Human clarification required.", stpRecord.TransactionId);

            // Optional: You could update the intake state here to move the file to a /Rejected or /Clarification folder
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An unexpected error occurred while executing Payload {TransactionId}.", stpRecord.TransactionId);
         }
         return new ParsedAndExecuteResult
         {
            ReadinessReport = readinessReport,
            SemanticModel = semanticModel
         };
      }

   }
}
