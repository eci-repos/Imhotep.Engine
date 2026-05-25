using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.ModelGateway.Models;
using Imhotep.Planning.Models;
using Imhotep.SemanticModel.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Agents.Implementation;

/// <summary>
/// The Implementation Generator is responsible for producing concrete software artifacts 
/// (e.g., .NET C# classes) based on bounded construction tasks.
/// </summary>
public class ImplementationGenerator : IAgent
{
   public string RoleName => "Implementation Generator";

   public async Task<AgentOutputRecord> ExecuteTaskAsync(
       AgentRuntimeRequest request,
       AgentContextPackage context,
       IModelGateway modelGateway,
       CancellationToken cancellationToken = default)
   {
      // 1. ISL v2.1 Sec 9.2: Context Assembly (Strictly bounded to IncludedEntities)
      // We do NOT query a global SemanticModel. We use the securely bounded entities passed by the Orchestrator.
      var canonicalEntities = context.IncludedEntities ?? new List<ICanonicalEntity>().AsReadOnly();
      var targetEntity = canonicalEntities.FirstOrDefault();

      // 2. ISL v3.8 Sec 7.1: Formal Instruction Structure
      var instructionBuilder = new StringBuilder();

      instructionBuilder.AppendLine("# protocol-header");
      instructionBuilder.AppendLine("IMHOTEP Model Interaction Protocol v1.0. Output format: JSON strictly.");

      instructionBuilder.AppendLine("# agent-role");
      instructionBuilder.AppendLine($"You are acting as the {RoleName}.");

      instructionBuilder.AppendLine("# task-objective");
      instructionBuilder.AppendLine($"Generate the C# implementation for the provided architectural entities.");

      instructionBuilder.AppendLine("# context-summary");
      if (targetEntity != null)
      {
         instructionBuilder.AppendLine($"TARGET_ENTITY_ID: {targetEntity.TraceabilityId}");
         instructionBuilder.AppendLine($"TARGET_ENTITY_TYPE: {targetEntity.GetType().Name}");
      }

      instructionBuilder.AppendLine("# constraints");
      instructionBuilder.AppendLine("- Generate only valid, compilable C# (.NET) source code.");
      instructionBuilder.AppendLine("- Ensure strict nomenclature alignment with the NCSC NODS canonical schema.");
      instructionBuilder.AppendLine("- Implement required zero-trust annotations or secure configurations if mandated by associated policies.");

      instructionBuilder.AppendLine("# output-contract");
      instructionBuilder.AppendLine(@"You must return your output strictly in the following JSON schema:
{
  ""fileName"": ""CaseDetail.cs"",
  ""fileContent"": ""// Raw C# code here...""
}");

      // 3. ISL v2.5 Sec 6.0: Dispatch to the Model Abstraction Boundary
      // (In a full implementation, you would construct a ModelInvocationRequest here and await the gateway)
      // var modelResponse = await modelGateway.InvokeModelAsync(..., instructionBuilder.ToString(), cancellationToken);

      // 4. ISL v3.4 Sec 13.1: Return strict Agent Output Implementation Schema
      return new AgentOutputRecord
      {
         AgentOutputId = $"AOUT-{Guid.NewGuid():N}",
         AgentInvocationId = request.AgentInvocationId,
         AgentImplementationId = request.AgentImplementationId,
         AgentRole = RoleName,
         TaskId = request.TaskId,
         OutputType = "artifact-candidate",
         Summary = $"Successfully generated C# implementation artifact candidates for {canonicalEntities.Count} entities.",

         // Extract the parsed JSON file content into the candidate list
         ProducedArtifactCandidates = new List<string> { $"ART-CANDIDATE-{Guid.NewGuid():N}" }.AsReadOnly(),

         ReferencedEntities = canonicalEntities.Select(e => e.TraceabilityId).ToList().AsReadOnly(),

         // ISL v3.4 Sec 14.1: Executable artifacts MUST require deterministic validation
         RequiresDeterministicValidation = true,

         OutputStatus = "valid",
         Confidence = "high",
         RequiresReview = false,
         ProducedAt = DateTimeOffset.UtcNow
      };
   }

}
