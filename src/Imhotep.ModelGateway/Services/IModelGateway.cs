
using System.Threading.Tasks;
using Imhotep.ModelGateway.Models;

namespace Imhotep.ModelGateway.Services;

/// <summary>
/// The standardized abstraction layer connecting IMHOTEP agents to underlying AI models.
/// Enforces the Model Interaction Protocol (ISL v3.8).
/// </summary>
public interface IModelGateway
{
   /// <summary>
   /// Executes a bounded reasoning transaction, normalizes the output, 
   /// and validates it against the output contract before returning it to the agent.
   /// </summary>
   Task<StructuredModelResponse> ExecuteReasoningTransactionAsync(StructuredModelRequest request);
}

