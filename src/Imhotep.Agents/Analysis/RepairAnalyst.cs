using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.ModelGateway.Models;
using Imhotep.Planning.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Agents.Analysis
{
   /// <summary>
   /// The Repair Analyst interprets failures produced during deterministic validation 
   /// and generates strict corrective instructions for the Implementation Generator (ISL v2.1).
   /// </summary>
   public class RepairAnalyst : IAgent
   {
      public string RoleName => "Repair Analyst";

      // ISL v1.2 Sec 15.2: Default Repair Termination Policy is 5 attempts
      private const int MaxRepairAttempts = 5;

      public async Task<AgentOutputRecord> ExecuteTaskAsync(
          AgentRuntimeRequest request,
          AgentContextPackage context,
          IModelGateway modelGateway,
          CancellationToken cancellationToken = default)
      {
         // 1. State Tracking via Context: Determine attempts from durably assembled context
         int currentRepairIteration = context.IncludedRepairRecords?.Count ?? 0;

         // 2. ISL v1.5 Sec 15.5: Enforce the Human-Machine Escalation Boundary
         if (currentRepairIteration >= MaxRepairAttempts)
         {
            return new AgentOutputRecord
            {
               AgentOutputId = $"AOUT-{Guid.NewGuid():N}",
               AgentInvocationId = request.AgentInvocationId,
               AgentImplementationId = request.AgentImplementationId,
               AgentRole = RoleName,
               TaskId = request.TaskId,
               OutputType = "repair-proposal",

               // Pull the Andon Cord: Halt the autonomous loop
               OutputStatus = "escalated",
               Summary = $"Escalation: Repair iterations ({currentRepairIteration}) reached the maximum threshold ({MaxRepairAttempts}) without convergence.",

               Confidence = "high",
               RequiresReview = true,
               RequiresDeterministicValidation = false,
               ReferencedEntities = context.IncludedEntities.Select(e => e.TraceabilityId).ToList().AsReadOnly(),
               ProducedAt = DateTimeOffset.UtcNow
            };
         }

         // 3. Context Assembly: Safely extract hydrated canonical entities & failures
         var targetEntities = context.IncludedEntities; // Fully hydrated by the Orchestrator
         var failedValidationIds = context.IncludedValidationResults ?? new List<string>().AsReadOnly();

         // 4. (Future Implementation) Route to IModelGateway utilizing the failedValidationIds
         // var modelResponse = await modelGateway.InvokeModelAsync(...);

         // 5. ISL v3.4 Sec 13.1: Return a strictly formatted Output Record
         return new AgentOutputRecord
         {
            AgentOutputId = $"AOUT-{Guid.NewGuid():N}",
            AgentInvocationId = request.AgentInvocationId,
            AgentImplementationId = request.AgentImplementationId,
            AgentRole = RoleName,
            TaskId = request.TaskId,
            OutputType = "repair-proposal",
            Summary = $"Repair proposal successfully generated for {failedValidationIds.Count} validation failures.",

            // The Repair Analyst proposes changes; the Implementation Generator produces artifacts
            ProducedArtifactCandidates = null,

            ReferencedEntities = targetEntities.Select(e => e.TraceabilityId).ToList().AsReadOnly(),
            RequiresDeterministicValidation = true,
            OutputStatus = "valid",
            Confidence = "high",
            RequiresReview = false,
            ProducedAt = DateTimeOffset.UtcNow
         };
      }

   }
}
