using System;
using System.Collections.Generic;
using System.Text;

namespace Imhotep.Specification.Feedback;


/// <summary>
/// Concrete implementation of IResponseDispatcher.
/// Routes the Advisory Collaboration feedback to the human authorities 
/// and halts the autonomous execution pipeline (The "Andon Cord").
/// </summary>
public class ResponseDispatcher : IResponseDispatcher
{
   public Task DispatchAsync(
      string transactionId, string formattedResponse, CancellationToken cancellationToken = default)
   {
      // Respect the runtime orchestrator's cancellation token
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(transactionId))
         throw new ArgumentException(
            "Transaction ID is required to route the response.", nameof(transactionId));

      // If the response is empty, the blueprint is Autonomous-Ready and no dispatch is needed.
      if (string.IsNullOrWhiteSpace(formattedResponse))
         return Task.CompletedTask;

      // In a full enterprise deployment, this integrates with webhooks, 
      // CI/CD pipelines, Git repository PR comments, or the Operational Dashboard.
      // For the MACS Proof-of-Concept, we log the escalation and formally halt the intake pipeline.

      Console.WriteLine($"[ESCALATION TRIGGERED - ANDON CORD PULLED] Transaction: {transactionId}");
      Console.WriteLine("Routing the following payload back to Human Governance Roles:");
      Console.WriteLine(formattedResponse);

      // Pulling the digital "Andon Cord" to strictly halt autonomous progression
      throw new HumanMachineEscalationException(transactionId, formattedResponse);
   }
}

/// <summary>
/// Custom exception to formally halt the pipeline and enforce human intervention.
/// </summary>
public class HumanMachineEscalationException : Exception
{
   public string TransactionId { get; }
   public string FormattedResponse { get; }

   public HumanMachineEscalationException(string transactionId, string formattedResponse)
       : base($"Autonomous execution halted for Transaction {transactionId}. Human clarifications required.\n\n{formattedResponse}")
   {
      TransactionId = transactionId;
      FormattedResponse = formattedResponse;
   }
}
