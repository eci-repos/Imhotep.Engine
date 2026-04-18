
using Imhotep.Macs.Models.Canonical;

/// <summary>
/// Traceability ID: ENT-NODS-CASE
/// NCSC NODS canonical table representing the court case.
/// Mapped from Psj_Court_Canonical.Case.CaseDetail
/// </summary>
public record CaseDetail : INodsAuditableEntity
{
   public required long CaseNo { get; init; } // Primary Key
   public string? CaseID { get; init; }
   public string? CaseNumber { get; init; }
   public string? TypeID { get; init; } // Foreign Key to CaseType
   public string? CaseTitle { get; init; }
   public DateTime? FiledDate { get; init; }
   public string? JudgeID { get; init; } // Foreign Key to Officer
   public long? CourtNo { get; init; } // Foreign Key to Organization

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

