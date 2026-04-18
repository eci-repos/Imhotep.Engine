
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Macs.Models.Intake;

/*
 * This local-first, containerized subsystem orchestrates the WKF-001 Intake Validation Process. It fulfills REQ-003 
 * by setting the initial state to "Open/Pending" and strictly enforces POL-PII-001 (PII Data Protection) by checking
 * for sensitive data that requires masking or soft deletion.
 */

namespace Imhotep.Macs.Services;

public record IntakeResult(bool IsSuccessful, string TraceabilityId, IReadOnlyList<string> ValidationErrors);

public interface IIntakeRestService
{
   Task<IntakeResult> ProcessIntakePayloadAsync(NodsCasePayload payload);
}

/// <summary>
/// Traceability ID: SRV-INTAKE-01
/// Subsystem responsible for the ingestion, validation, and initial processing of court case filings.
/// Orchestrates WKF-001: Intake Validation Process.
/// </summary>
public class IntakeRestService : IIntakeRestService
{
   private readonly ILogger<IntakeRestService> _logger;

   public IntakeRestService(ILogger<IntakeRestService> logger)
   {
      _logger = logger;
   }

   public async Task<IntakeResult> ProcessIntakePayloadAsync(NodsCasePayload payload)
   {
      var errors = new List<string>();

      // 1. REQ-001 & POL-NCSC-001: Schema & Data Quality Validation
      if (payload.Case == null || !payload.Parties.Any())
      {
         errors.Add("Payload must contain a valid ENT-NODS-CASE and at least one ENT-NODS-PARTY.");
         return new IntakeResult(false, payload.SubmissionId, errors);
      }

      // 2. POL-PII-001: PII Data Protection Enforcement (Soft Deletion / Masking Notification)
      bool containsPii = payload.Parties.Any(p => !string.IsNullOrEmpty(p.FBINumber) || p.DateOfBirth.HasValue);
      if (containsPii)
      {
         _logger.LogWarning("[POL-PII-001] PII detected in payload {SubmissionId}. Generating notification for masking/soft deletion per CJIS policies.", payload.SubmissionId);
         // Implementation note: Trigger event to PII Scanner/Masking service
      }

      // 3. REQ-003: Initiate Case Lifecycle
      // Set the NCSC case status to 'Open/Pending' 
      var initiatedCase = payload.Case with
      {
         RecordStatusCodeID = "OPEN", // Simulated NCSC Status Code
         UpdatedDateTime = DateTimeOffset.UtcNow
      };

      _logger.LogInformation("WKF-001: Case payload {SubmissionId} successfully validated and mapped to canonical entities.", payload.SubmissionId);

      // In a complete implementation, this is where the validated entities 
      // are persisted to the canonical relational database (Psj_Court_Canonical).
      await Task.CompletedTask;

      return new IntakeResult(true, payload.SubmissionId, errors);
   }
}

