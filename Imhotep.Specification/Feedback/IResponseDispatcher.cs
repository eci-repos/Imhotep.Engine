using System;
using System.Collections.Generic;
using System.Text;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Feedback;

public interface IResponseDispatcher
{
   /// <summary>
   /// Safely dispatches the reviewed specification or clarification requests back 
   /// to Human Governance Roles.
   /// </summary>
   Task DispatchResponseAsync(
      string targetIdentifier, string formattedResponse, 
      CancellationToken cancellationToken = default);
}
