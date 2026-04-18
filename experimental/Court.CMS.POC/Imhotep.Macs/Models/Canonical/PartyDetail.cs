
using Imhotep.Macs.Models.Canonical;

/// <summary>
/// Traceability ID: ENT-NODS-PARTY
/// NCSC NODS canonical table representing participants.
/// Constrained by POL-PII-001 (PII Masking/Soft Deletion) and POL-CJIS-001 (Encryption).
/// Mapped from Psj_Court_Canonical.Party.PartyDetail
/// </summary>
public record PartyDetail : INodsAuditableEntity
{
   public required long PartyNo { get; init; } // Primary Key
   public string? PartyID { get; init; }

   public string? GenderCodeID { get; init; }
   public string? RaceCodeID { get; init; }
   public string? Height { get; init; }
   public string? Weight { get; init; }

   // PII Fields - Must be encrypted at rest and masked during soft-deletion
   public DateTime? DateOfBirth { get; init; }
   public DateTime? DateOfDeath { get; init; }
   public string? StateID { get; init; }
   public string? FBINumber { get; init; }

   // Auditable Entity Implementation
   public required string SessionID { get; init; }
   public required string TenantID { get; init; }
   public required string DataOwnerID { get; init; }
   public required string DataSourceID { get; init; }
   public required DateTimeOffset CreatedDateTime { get; init; }
   public required DateTimeOffset UpdatedDateTime { get; init; }
   public required string RecordStatusCodeID { get; init; }
   public required DateTimeOffset RecordStatusDateTime { get; init; }
}

