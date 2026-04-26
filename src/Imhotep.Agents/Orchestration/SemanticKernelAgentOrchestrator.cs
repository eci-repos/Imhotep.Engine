using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.Planning.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Agents.Orchestration
{
   /// <summary>
   /// Coordinates specialized reasoning agents using Microsoft Semantic Kernel, strictly enforcing 
   /// the Model Interaction Protocol (ISL v3.8) through Structured Transaction Payloads.
   /// </summary>
   public class SemanticKernelAgentOrchestrator : IAgentOrchestrator
   {
      private readonly Kernel _kernel;
      private readonly ILogger<SemanticKernelAgentOrchestrator> _logger;

      public SemanticKernelAgentOrchestrator(Kernel kernel, ILogger<SemanticKernelAgentOrchestrator> logger)
      {
         _kernel = kernel;
         _logger = logger;
      }

      public async Task<AgentResult> DispatchTaskAsync(ConstructionTask task, AgentContext context, CancellationToken cancellationToken = default)
      {
         _logger.LogInformation("Orchestrator dispatching Task {TaskId} to specialized agent pipeline.", task.TaskId);

         // 1. Context Assembly: Determine which Agent Role should handle the task
         string assignedAgentRole = DetermineAgentRole(task, context);

         // 2. Build the Structured Transaction Payload (STP) using YAML Frontmatter and strict Headers
         string structuredPayload = BuildStructuredTransactionPayload(task, assignedAgentRole, context);

         try
         {
            // 3. Model Invocation: Send the strict payload to Semantic Kernel (Local or Cloud Model)
            _logger.LogDebug("Invoking Semantic Kernel for {AgentRole} via Structured Reasoning Transaction.", assignedAgentRole);

            var kernelResult = await _kernel.InvokePromptAsync(structuredPayload, cancellationToken: cancellationToken);
            string responseText = kernelResult.GetValue<string>() ?? string.Empty;

            // 4. Enforce Output Contracts and Advisory Collaboration (ISL v3.8)
            return EvaluateOutputContract(task.TaskId, responseText);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Semantic Kernel invocation failed for Task {TaskId}.", task.TaskId);
            return new AgentResult { IsSuccess = false, ErrorMessage = "ESCALATION_REQUIRED" }; // Pull the Andon Cord
         }
      }

      /// <summary>
      /// Formats the request strictly abandoning prose in favor of the ISL v3.8 Payload format.
      /// </summary>
      private string BuildStructuredTransactionPayload(ConstructionTask task, string agentRole, AgentContext context)
      {
         var sb = new StringBuilder();

         // YAML Frontmatter Metadata
         sb.AppendLine("---");
         sb.AppendLine($"TRANSACTION_ID: {task.TaskId}");
         sb.AppendLine($"AGENT_ROLES: [{agentRole}]");
         sb.AppendLine($"TARGET_TRACEABILITY_ID: {task.TargetTraceabilityId}");
         sb.AppendLine("TARGET_ARCHITECTURE: Minimal Autonomous Construction System (MACS) .NET REST Service");
         sb.AppendLine("---");

         // Strict Markdown Headers for Context Assembly
         sb.AppendLine("# CONTEXT ASSEMBLY:");
         sb.AppendLine(task.Description);

         if (!string.IsNullOrEmpty(context.DeterministicValidationFeedback))
         {
            sb.AppendLine("\n**PRIOR VALIDATION FAILURE (REPAIR CONTEXT):**");
            sb.AppendLine(context.DeterministicValidationFeedback);
         }

         // Explicit Constraints
         sb.AppendLine("\n# OPERATIONAL CONSTRAINTS:");
         sb.AppendLine("- You are operating under a strict Zero-Trust Model. Do not guess.");
         sb.AppendLine("- Ensure Explicit Edge Creation: Cross-reference Traceability Identifiers (e.g., REQ-001).");
         sb.AppendLine("- Prohibited Artifacts: DO NOT generate UI mockups, conversational text, or vendor-specific hacks.");

         // The Strict Output Contract (The Dual-Audience Requirement)
         sb.AppendLine("\n# OUTPUT CONTRACT:");
         sb.AppendLine("Output ONLY a strict ISL Markdown, JSON, or YAML document.");
         sb.AppendLine("Absolutely NO conversational preambles, acknowledgments, or concluding remarks are permitted.");
         sb.AppendLine("If mandatory jurisdictional boundaries, compliance standards, or deterministic validation rules are ambiguous, DO NOT ask conversational questions.");
         sb.AppendLine("Instead, append a strictly formatted `### CLARIFICATIONS REQUIRED` block at the very end of your response.");

         return sb.ToString();
      }

      /// <summary>
      /// Validates that the agent adhered to the strict formatting and checks for exceptions.
      /// Returns the key-value dictionary of artifacts.
      /// </summary>
      private AgentResult EvaluateOutputContract(string taskId, string rawResponse)
      {
         // 5. Catch Advisory Collaboration ("### CLARIFICATIONS REQUIRED")
         if (rawResponse.Contains("### CLARIFICATIONS REQUIRED"))
         {
            _logger.LogWarning("Task {TaskId} triggered Advisory Collaboration. Missing canonical context detected.", taskId);

            // Return failure but specifically flag that human review is needed to resolve the missing context
            return new AgentResult
            {
               IsSuccess = false,
               ErrorMessage = "ESCALATION_REQUIRED",
               GeneratedArtifacts = new Dictionary<string, string>
                    {
                        { "ClarificationRequest", rawResponse }
                    }
            };
         }

         // 6. Strip accidental conversational filler if the model hallucinated slightly
         string cleanedArtifact = CleanModelOutput(rawResponse);

         _logger.LogInformation("Task {TaskId} produced a valid structured artifact.", taskId);

         return new AgentResult
         {
            IsSuccess = true,
            GeneratedArtifacts = new Dictionary<string, string>
                { 
                    // In a more advanced implementation, the agent would return a JSON dict of { "filename": "content" }.
                    // For the foundational MACS pipeline, we bind the output to the TaskId.
                    { $"ART-{taskId}-PrimaryOutput", cleanedArtifact }
                }
         };
      }

      private string DetermineAgentRole(ConstructionTask task, AgentContext context)
      {
         if (task.ExecutionStatus == PlanTaskStatus.InRepair || !string.IsNullOrEmpty(context.DeterministicValidationFeedback))
            return "Repair Analyst";

         if (task.Description.Contains("Generate API") || task.Description.Contains("Generate service logic"))
            return "Implementation Generator";

         return "Architecture Planner";
      }

      private string CleanModelOutput(string response)
      {
         var trimmed = response.Trim();
         if (trimmed.StartsWith("```") && trimmed.EndsWith("```"))
         {
            var lines = trimmed.Split('\n');
            return string.Join('\n', lines, 1, lines.Length - 2).Trim();
         }
         return trimmed;
      }
   }
}
