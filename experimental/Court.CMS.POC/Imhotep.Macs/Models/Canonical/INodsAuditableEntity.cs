
using System;

namespace Imhotep.Macs.Models.Canonical;

/// <summary>
/// Ensures all NCSC NODS entities include mandatory auditing, multi-tenancy, 
/// and soft-deletion (RecordStatusCodeID) structures for POL-PII-001 compliance.
/// </summary>
public interface INodsAuditableEntity
{
   string SessionID { get; init; }
   string TenantID { get; init; }
   string DataOwnerID { get; init; }
   string DataSourceID { get; init; }
   DateTimeOffset CreatedDateTime { get; init; }
   DateTimeOffset UpdatedDateTime { get; init; }

   /// <summary>
   /// Used to enforce soft-deletion and PII masking policies (POL-PII-001).
   /// </summary>
   string RecordStatusCodeID { get; init; }
   DateTimeOffset RecordStatusDateTime { get; init; }
}

