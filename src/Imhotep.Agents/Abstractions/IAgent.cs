using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Imhotep.Planning.Models;
using Imhotep.Agents.Models;
using Imhotep.SemanticModel.Graph;
using Imhotep.ModelGateway.Abstractions;

namespace Imhotep.Agents.Abstractions
{
   /// <summary>
   /// ISL v3.4 Section 8.0: Agent Runtime Interface
   /// Defines the contract for all role-bounded reasoning agents.
   /// </summary>
   public interface IAgent
   {
      /// <summary>
      /// The formal ISL Agent Role this implementation fulfills (e.g., "Repair Analyst").
      /// </summary>
      string RoleName { get; }

      /// <summary>
      /// Executes the bounded reasoning transaction based on a strict request contract.
      /// </summary>
      Task<AgentOutputRecord> ExecuteTaskAsync(
          AgentRuntimeRequest request,
          AgentContextPackage context,
          IModelGateway modelGateway,
          CancellationToken cancellationToken = default);
   }
}
