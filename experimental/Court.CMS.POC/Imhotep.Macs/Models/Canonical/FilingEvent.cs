
using Imhotep.Macs.Models.Canonical;

/// <summary>
/// Traceability ID: ENT-NODS-FILING
/// NCSC NODS canonical table representing specific filing events.
/// Mapped from Psj_Court_Canonical.Case.FilingEvent
/// </summary>
public record FilingEvent : INodsAuditableEntity
{
   public required long FilingNo { get; init; } // Primary Key
   public required long CaseNo { get; init; } // Foreign Key to CaseDetail
   public long? AuthorityNo { get; init; } // Foreign Key to Organization
   public string? FilingID { get; init; }
   public long? FilingPartyNo { get; init; } // Foreign Key to PartyDetail
   public string? TypeID { get; init; } // Foreign Key to FilingEventType
   public long? EventNo { get; init; } // Foreign Key to Event

   public DateTime? DateFiled { get; init; }
   public DateTime? DateSubmitted { get; init; }
   public string? StatusCode { get; init; }
   public DateTimeOffset? StatusDate { get; init; }

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

