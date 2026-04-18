

using Imhotep.Macs.Models.Canonical;

/// <summary>
/// Traceability ID: ENT-NODS-CHARGE
/// NCSC NODS canonical table representing filing charges and related statutes.
/// Mapped from Psj_Court_Canonical.Charge.ChargeDetail
/// </summary>
public record ChargeDetail : INodsAuditableEntity
{
    public required long ChargeNo { get; init; } // Primary Key
    public required long CaseNo { get; init; } // Foreign Key to CaseDetail
    public string? ChargeID { get; init; }
    
    public DateTime? FillingDate { get; init; }
    public DateTime? ArrestDate { get; init; }
    public string? ArrestTrackID { get; init; }
    public DateTime? DispositionDate { get; init; }
    public string? OfficerID { get; init; }
    public DateTime? OffenseDate { get; init; }
    public string? OffenseLocationID { get; init; }
    
    public string? ChargeNumber { get; init; }
    public string? ChargeDescription { get; init; }
    public string? DrugTypeID { get; init; }
    public string? IncidentZoneTypeID { get; init; }
    public string? JurisdictionID { get; init; } // Foreign Key
    public string? OffenseID { get; init; } // Foreign Key
    
    public string? ChargeTrackNumber { get; init; }
    public string? ChargeTrackSequence { get; init; }
    public string? ChargeMitigator { get; init; }
    
    public DateTime? AmendedDate { get; init; }
    public string? AmendedReason { get; init; }
    public bool? WasChargeChanged { get; init; }
    public bool? Deleted { get; init; }
    
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

