using System;
using System.Collections.Generic;
using System.Text;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Feedback;

/// <summary>
/// Defines the contract for dispatching formatted responses back to the 
/// human governance team, enforcing the Human-Machine Escalation pathway.
/// </summary>
public interface IResponseDispatcher
{
   /// <summary>
   /// Dispatches the formatted clarification block or escalation payload.
   /// </summary>
   Task DispatchAsync(string transactionId, string formattedResponse, 
      CancellationToken cancellationToken = default);
}
