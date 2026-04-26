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
      private const int MaxRepairAttempts = 3;

      public async Task<AgentResult> ExecuteTaskAsync(
          ConstructionTask task,
          AgentContext context,
          IModelGateway modelGateway,
          CancellationToken cancellationToken = default)
      {
         // 1. Enforce the Human-Machine Escalation Boundary (The "Andon Cord")
         if (task.RepairAttempts >= MaxRepairAttempts)
         {
            return GenerateEscalationPayload(task, context);
         }

         // 2. Context Assembly: Feed the structured validation errors to the model
         var targetEntity = context.SemanticModel.GetEntityById(task.TargetTraceabilityId);

         string contextAssembly = $@"
TARGET_ENTITY: {targetEntity?.TraceabilityId}
ENTITY_NAME: {targetEntity?.Name}
FAILED_VALIDATION_RULE: {context.ValidationRuleId}
DETERMINISTIC_ERRORS: 
{context.DeterministicValidationFeedback}

PRIOR_ARTIFACT_STATE:
{FormatPriorArtifacts(context.PriorArtifacts)}";

         // 3. Operational Constraints
         string operationalConstraints = @"
- Analyze the provided compilation or validation errors.
- Do not generate full source code. Instead, generate precise, targeted modifications.
- Ensure the proposed fix adheres strictly to the canonical entity definition and does not violate other policies.";

         // 4. Output Contract (Strict JSON)
         string outputContractSchema = @"
You must return your output strictly in the following JSON schema:
{
  ""diagnosis"": ""Brief explanation of why the validation failed."",
  ""repairInstructions"": ""Explicit instructions on what lines to change or dependencies to add.""
}";

         // 5. Construct the Structured Transaction Request
         var request = new StructuredModelRequest
         {
            TransactionId = $"TX-REPAIR-{task.TaskId}-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            TaskId = task.TaskId,
            AgentRole = RoleName,
            ContextAssembly = contextAssembly,
            OperationalConstraints = operationalConstraints,
            OutputContractSchema = outputContractSchema
         };

         try
         {
            // 6. Execute Reasoning Transaction
            StructuredModelResponse response = await modelGateway.ExecuteReasoningTransactionAsync(request, cancellationToken);

            if (response.IsValidContract)
            {
               return new AgentResult
               {
                  IsSuccess = true,
                  StructuredOutput = response.NormalizedOutput, // The instructions for the Implementation Generator
                  GeneratedArtifacts = new Dictionary<string, string>() // The Repair Analyst does not write code directly
               };
            }
            else
            {
               return new AgentResult { IsSuccess = false, ErrorMessage = "Failed output contract validation." };
            }
         }
         catch (Exception ex)
         {
            return new AgentResult { IsSuccess = false, ErrorMessage = $"Reasoning transaction failed: {ex.Message}" };
         }
      }

      /// <summary>
      /// Halts autonomous execution and routes a formal escalation payload to human operators.
      /// </summary>
      private AgentResult GenerateEscalationPayload(ConstructionTask task, AgentContext context)
      {
         string escalationJson = $@"
{{
  ""escalationType"": ""Unresolvable Structural Conflict"",
  ""traceabilityPath"": ""{task.TargetTraceabilityId} -> {context.ValidationRuleId}"",
  ""failureContext"": ""Exhausted {MaxRepairAttempts} repair cycles. Manual architectural intervention required.""
}}";
         return new AgentResult
         {
            IsSuccess = false,
            ErrorMessage = "ESCALATION_REQUIRED",
            StructuredOutput = escalationJson
         };
      }

      private string FormatPriorArtifacts(Dictionary<string, string> artifacts)
      {
         // Utility to format the broken code for the prompt context
         return string.Join("\n", artifacts);
      }
   }
}
