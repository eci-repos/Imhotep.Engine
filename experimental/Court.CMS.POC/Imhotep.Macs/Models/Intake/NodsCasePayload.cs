
using System.Collections.Generic;
using Imhotep.Macs.Models.Canonical;

/*
 * Define the composite payload structure that the ACT-001 (External E-Filing System) submits. 
 * This explicitly bundles the canonical data entities previously defined.
 * (see Imhotep.Macs.Models.Canonical namespace for CaseDetail, PartyDetail, ChargeDetail, 
 * FilingEvent, HearingDetail records)
 */

namespace Imhotep.Macs.Models.Intake;

/// <summary>
/// Represents the structured NCSC NODS-compliant JSON payload submitted by ACT-001.
/// </summary>
public record NodsCasePayload
{
   public required string SubmissionId { get; init; }

   // Core NCSC Entities
   public required CaseDetail Case { get; init; }
   public IReadOnlyList<PartyDetail> Parties { get; init; } = new List<PartyDetail>();
   public IReadOnlyList<ChargeDetail> Charges { get; init; } = new List<ChargeDetail>();
   public IReadOnlyList<FilingEvent> Filings { get; init; } = new List<FilingEvent>();
   public IReadOnlyList<HearingDetail> Hearings { get; init; } = new List<HearingDetail>();
}

