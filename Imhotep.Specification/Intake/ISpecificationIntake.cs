

// -------------------------------------------------------------------------------------------------
namespace Imhotep.Specification.Intake;

public interface ISpecificationIntake
{
   /// <summary>
   /// Securely receives the raw Structured Transaction Payload (STP) from an external source.
   /// </summary>
   Task<string> ReceivePayloadAsync(
      string sourceIdentifier, CancellationToken cancellationToken = default);
}
