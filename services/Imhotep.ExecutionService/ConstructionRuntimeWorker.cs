using Imhotep.SemanticModel.Graph;
using Imhotep.Specification.Feedback;
using Imhotep.Specification.Intake;
using Imhotep.Specification.Pipeline;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imhotep.ExecutionService;

public class ConstructionRuntimeWorker : BackgroundService
{
   private readonly ILogger<ConstructionRuntimeWorker> _logger;
   private readonly SpecificationIntakePipeline _intakePipeline;
   private readonly ISpecificationIntake _specificationIntake;

   // Assuming an interface that fetches the raw STP from the Artifact Repository
   // private readonly IArtifactRepository _artifactRepository; 

   public ConstructionRuntimeWorker(
       ILogger<ConstructionRuntimeWorker> logger,
       SpecificationIntakePipeline intakePipeline,
       ISpecificationIntake specificationIntake)
   {
      _logger = logger;
      _intakePipeline = intakePipeline;
      _specificationIntake = specificationIntake;
   }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      _logger.LogInformation("IMHOTEP Execution Runtime initiated at: {time}", DateTimeOffset.Now);

      while (!stoppingToken.IsCancellationRequested)
      {
         try
         {
            _logger.LogInformation("Scanning intake boundary for pending STPs...");

            // 1. FORMAL AUTOMATION: Pull from the intake abstraction rather than hardcoding file reads
            var pendingPayloads = await _specificationIntake.GetPendingPayloadsAsync(stoppingToken);

            foreach (var stpRecord in pendingPayloads)
            {
               _logger.LogInformation("Payload detected: {TransactionId}. Executing Specification Intake Pipeline...", stpRecord.TransactionId);

               // 2. The Intake Pipeline natively handles Parsing -> Normalization -> Evaluation -> Feedback
               var semanticModel = await _intakePipeline.ProcessPayloadAsync(stpRecord.RawMarkdown, stoppingToken);

               // 3. If successful, update the physical state (e.g., move the file to /InProgress)
               await _specificationIntake.UpdatePayloadStateAsync(stpRecord.TransactionId, IntakeState.InProgress, stoppingToken);

               _logger.LogInformation("Approval Gates cleared. Specification {TransactionId} is Autonomous-Ready.", stpRecord.TransactionId);

               // Handoff to Day-2 Subsystems (To be implemented)
               // var taskGraph = await _planningEngine.GenerateTaskGraphAsync(semanticModel);
               // await _agentOrchestrator.ExecuteConstructionPlanAsync(taskGraph, stoppingToken);

               _logger.LogInformation("Construction pipeline handoff complete for {TransactionId}.", stpRecord.TransactionId);
            }
         }
         catch (HumanMachineEscalationException ex)
         {
            // The pipeline mathematically proved a failure in readiness and pulled the Andon Cord.
            _logger.LogWarning(ex, "Advisory Collaboration triggered: Specification requires human clarification. Execution halted for this payload.");

            // If you have access to the TransactionId here, you would transition its state:
            // await _specificationIntake.UpdatePayloadStateAsync(failedTransactionId, IntakeState.Escalated, stoppingToken);
         }
         catch (Exception ex)
         {
            // Catch-all for severe runtime crashes (e.g., infrastructure failures)
            _logger.LogError(ex, "Systemic Exception: Structural conflict detected in execution runtime.");
         }

         // Polling interval for the background worker (Consider injecting this via IOptions<RuntimeConfiguration> later!)
         await Task.Delay(10000, stoppingToken);
      }
   }

}
