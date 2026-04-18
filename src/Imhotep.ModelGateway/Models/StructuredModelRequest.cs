
using System.Collections.Generic;

namespace Imhotep.ModelGateway.Models;

/// <summary>
/// Represents a bounded, formal reasoning task passed to an AI model, 
/// strictly prohibiting open-ended conversational prompts.
/// </summary>
public record StructuredModelRequest
{
   public required string TransactionId { get; init; }

   /// <summary>
   /// The specific cognitive lens applied to the task (e.g., "Architecture Planner", "Repair Analyst").
   /// </summary>
   public required string AgentRole { get; init; }

   public required string TaskId { get; init; }

   /// <summary>
   /// The precise data (specifications, schemas, failures) the model is allowed to reason over.
   /// </summary>
   public required string ContextAssembly { get; init; }

   /// <summary>
   /// Strict operational and governance rules the model must follow during generation.
   /// </summary>
   public required string OperationalConstraints { get; init; }

   /// <summary>
   /// The exact schema (e.g., JSON schema or ISL Markdown format) the model's output must match.
   /// </summary>
   public required string OutputContractSchema { get; init; }
}

/// <summary>
/// The normalized and validated response returned by the model provider.
/// </summary>
public record StructuredModelResponse
{
   /// <summary>
   /// The clean, machine-parseable output with all conversational filler removed.
   /// </summary>
   public required string NormalizedOutput { get; init; }

   /// <summary>
   /// Indicates if the response strictly matched the requested OutputContractSchema.
   /// </summary>
   public required bool IsValidContract { get; init; }

   /// <summary>
   /// Any structural validation errors preventing the platform from using the output.
   /// </summary>
   public IReadOnlyList<string> ValidationErrors { get; init; } = new List<string>();
}

