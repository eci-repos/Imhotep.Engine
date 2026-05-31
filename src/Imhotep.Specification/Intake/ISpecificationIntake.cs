namespace Imhotep.Specification.Intake;

/// <summary>
/// ISL v3.0: Defines the intake boundary for the Specification Engine.
/// Abstracts the physical mechanism (e.g., Git, File System, REST API) used by human teams 
/// to submit Structured Transaction Payloads (STPs).
/// </summary>
public interface ISpecificationIntake
{
   // --- SINGLE SUBMISSION (Evolution of your existing method) ---

   /// <summary>
   /// Securely receives a specific raw Structured Transaction Payload (STP) from an external source.
   /// Useful for targeted REST API or CLI submissions.
   /// </summary>
   Task<PendingPayloadRecord?> ReceivePayloadAsync(
       string sourceIdentifier, CancellationToken cancellationToken = default);

   // --- AUTOMATED BACKGROUND POLLING ---

   /// <summary>
   /// Scans the intake boundary (e.g., a pending directory or Git queue) for all newly submitted STPs.
   /// Useful for the Execution Runtime's BackgroundService to autonomously pull work.
   /// </summary>
   Task<IEnumerable<PendingPayloadRecord>> GetPendingPayloadsAsync(
       CancellationToken cancellationToken = default);

   // --- INTAKE STATE MANAGEMENT ---

   /// <summary>
   /// Updates the state of the payload at the intake boundary (e.g., moving the file to an 'in-progress' folder,
   /// or tagging a Git commit) to mathematically prevent duplicate intake processing.
   /// </summary>
   Task UpdatePayloadStateAsync(
       string transactionId, IntakeState newState, CancellationToken cancellationToken = default);
}

// The Intake layer only cares about state and raw text.
public enum IntakeState { Pending, InProgress, Admitted, Rejected, Escalated }

public record PendingPayloadRecord(
   string TransactionId,
   string RawMarkdown,
   string SourceIdentifier,
   DateTimeOffset ReceivedAt
);
