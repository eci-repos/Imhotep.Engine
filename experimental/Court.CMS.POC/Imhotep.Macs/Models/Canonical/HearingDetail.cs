
using Imhotep.Macs.Models.Canonical;

/// <summary>
/// Traceability ID: ENT-NODS-HEARING
/// NCSC NODS canonical table representing specific scheduled hearing details.
/// Mapped from Psj_Court_Canonical.Activity.HearingDetail
/// </summary>
public record HearingDetail : INodsAuditableEntity
{
   public required string HearingID { get; init; } // Primary Key
   public required long CaseNo { get; init; } // Foreign Key to CaseDetail
   public string? CaseNumber { get; init; }
   public string? CaseTypeID { get; init; } // Foreign Key to CaseType
   public string? CaseTitle { get; init; }
   public string? CourtName { get; init; }
   public string? Defendant { get; init; }
   public string? AttorneyName { get; init; }

   public string? TypeID { get; init; } // Foreign Key to HearingType
   public string? HearingFlagID { get; init; } // Foreign Key to HearingFlag
   public DateTime? HearingDate { get; init; }
   public string? HearingSequence { get; init; }
   public string? SesssionDescription { get; init; } // Note: Retained original schema typo 'Sesssion' [10]
   public string? StartTime { get; init; }
   public string? SessionEndTime { get; init; }

   public string? JudgeID { get; init; } // Foreign Key to Officer
   public long? DepartmentNo { get; init; } // Foreign Key to Organization
   public string? ResultCodeID { get; init; } // Foreign Key to HearingResultCode

   public string? CancelledReasonCode { get; init; }
   public string? RescheduleReasonCode { get; init; }
   public DateTimeOffset? CancelledDateTime { get; init; }

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

