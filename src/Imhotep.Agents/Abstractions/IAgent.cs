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
   /// The standard contract for all specialized reasoning agents (ISL v2.1).
   /// </summary>
   public interface IAgent
   {
      string RoleName { get; } // e.g., "Implementation Generator", "Repair Analyst"

      Task<AgentResult> ExecuteTaskAsync(
          ConstructionTask task,
          AgentContext context,
          IModelGateway modelGateway,
          CancellationToken cancellationToken = default);
   }
}
