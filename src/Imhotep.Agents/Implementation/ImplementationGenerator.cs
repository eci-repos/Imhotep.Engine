using Imhotep.Agents.Abstractions;
using Imhotep.Agents.Models;
using Imhotep.ModelGateway.Abstractions;
using Imhotep.ModelGateway.Models;
using Imhotep.Planning.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Agents.Implementation
{
   /// <summary>
   /// The Implementation Generator is responsible for producing concrete software artifacts 
   /// (e.g., .NET C# classes) based on bounded construction tasks.
   /// </summary>
   public class ImplementationGenerator : IAgent
   {
      public string RoleName => "Implementation Generator";

      public async Task<AgentResult> ExecuteTaskAsync(
          ConstructionTask task,
          AgentContext context,
          IModelGateway modelGateway,
          CancellationToken cancellationToken = default)
      {
         // 1. Context Assembly (ISL v3.8)
         // Extract the specific entity this task is targeting (e.g., ENT-NODS-CASE)
         var targetEntity = context.SemanticModel.GetEntityById(task.TargetTraceabilityId);

         string contextAssembly = $@"
TARGET_ENTITY: {targetEntity?.TraceabilityId}
ENTITY_NAME: {targetEntity?.Name}
ENTITY_DESCRIPTION: {targetEntity?.Description}
TASK_DESCRIPTION: {task.Description}
TARGET_ARCHITECTURE: {context.SemanticModel.TargetArchitecture}";

         // 2. Operational Constraints
         string operationalConstraints = @"
- Generate only valid, compilable C# (.NET) source code.
- Ensure strict nomenclature alignment with the NCSC NODS canonical schema.
- Implement required zero-trust annotations or secure configurations if mandated by associated policies.";

         // 3. Output Contract
         string outputContractSchema = @"
You must return your output strictly in the following JSON schema:
{
  ""fileName"": ""CaseDetail.cs"",
  ""fileContent"": ""// Raw C# code here...""
}";

         // 4. Construct the Structured Transaction Request
         var request = new StructuredModelRequest
         {
            TransactionId = $"TX-{task.TaskId}-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            TaskId = task.TaskId,
            AgentRole = RoleName,
            ContextAssembly = contextAssembly,
            OperationalConstraints = operationalConstraints,
            OutputContractSchema = outputContractSchema
         };

         try
         {
            // 5. Execute the Reasoning Transaction via the Model Gateway
            StructuredModelResponse response = await modelGateway.ExecuteReasoningTransactionAsync(request, cancellationToken);

            if (response.IsValidContract)
            {
               // If successful, parse the JSON to extract the file name and content
               // (Assuming a simple JSON deserializer utility here)
               var generatedArtifact = ParseGeneratedCode(response.NormalizedOutput);

               return new AgentResult
               {
                  IsSuccess = true,
                  GeneratedArtifacts = new Dictionary<string, string>
                        {
                            { generatedArtifact.FileName, generatedArtifact.FileContent }
                        },
                  StructuredOutput = response.NormalizedOutput
               };
            }
            else
            {
               return new AgentResult
               {
                  IsSuccess = false,
                  ErrorMessage = $"Failed output contract validation: {string.Join(", ", response.ValidationErrors)}"
               };
            }
         }
         catch (Exception ex)
         {
            return new AgentResult
            {
               IsSuccess = false,
               ErrorMessage = $"Reasoning transaction failed: {ex.Message}"
            };
         }
      }

      private (string FileName, string FileContent) ParseGeneratedCode(string jsonOutput)
      {
         // Placeholder for JSON deserialization logic mapping to the output contract.
         // e.g., var parsed = JsonSerializer.Deserialize<GeneratedCodeTemplate>(jsonOutput);
         return ("GeneratedClass.cs", "public class GeneratedClass { }");
      }
   }
}
