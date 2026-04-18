using Imhotep.Planning.Services;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Agents.Models;

/// <summary>
/// Represents the structured input required for an agent to perform its reasoning task.
/// This ensures the agent operates on bounded context rather than unbounded chat.
/// </summary>
public record AgentContext
{
   /// <summary>
   /// The specific construction task assigned to the agent.
   /// </summary>
   public required ConstructionTask Task { get; init; }

   /// <summary>
   /// The canonical blueprint providing architectural context.
   /// </summary>
   public required CanonicalSemanticModel SemanticModel { get; init; }

   /// <summary>
   /// Any validation failures passed to the agent during a repair cycle.
   /// </summary>
   public IReadOnlyList<string> ValidationFailures { get; init; } = new List<string>();
}

/// <summary>
/// Represents the strict output contract returned by an agent.
/// </summary>
public record AgentResponse
{
   /// <summary>
   /// The concrete software artifact produced (e.g., C# source code, JSON schema).
   /// </summary>
   public required string GeneratedArtifact { get; init; }

   /// <summary>
   /// The TraceabilityId of the specification entity this artifact fulfills.
   /// </summary>
   public required string TargetTraceabilityId { get; init; }

   /// <summary>
   /// Indicates whether the reasoning task completed successfully.
   /// </summary>
   public bool IsSuccessful { get; init; }

   /// <summary>
   /// Any structured context regarding failures or required repairs.
   /// </summary>
   public string DiagnosticContext { get; init; } = string.Empty;
}

