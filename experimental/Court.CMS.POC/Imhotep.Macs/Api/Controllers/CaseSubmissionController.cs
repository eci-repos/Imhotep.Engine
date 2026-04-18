
using Imhotep.Macs.Models.Intake;
using Imhotep.Macs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

/*
 * This is the RESTful communication boundary. To satisfy POL-NIST-001, it uses ASP.NET Core's built-in [Authorize] 
 * attribute configured for JWT Bearer tokens (Entra ID). This ensures that unauthenticated requests are 
 * deterministically rejected before reaching the business logic, satisfying the VAL-003 Identity Provider 
 * Testing Plugin.
 */

namespace Imhotep.Macs.Api.Controllers;

/// <summary>
/// Traceability ID: INT-001
/// Case Submission API - A RESTful communication boundary accepting NCSC NODS-compliant JSON payloads.
/// </summary>
[ApiController]
[Route("api/v1/intake")]
// POL-NIST-001 [Mandatory]: Strict authentication utilizing OAuth with Entra ID.
[Authorize(AuthenticationSchemes = "Bearer")]
public class CaseSubmissionController : ControllerBase
{
   private readonly IIntakeRestService _intakeService;
   private readonly ILogger<CaseSubmissionController> _logger;

   public CaseSubmissionController(IIntakeRestService intakeService, ILogger<CaseSubmissionController> logger)
   {
      _intakeService = intakeService;
      _logger = logger;
   }

   [HttpPost("submit")]
   public async Task<IActionResult> SubmitCaseAsync([FromBody] NodsCasePayload payload)
   {
      _logger.LogInformation("Received case ingestion request. SubmissionID: {SubmissionId}", payload.SubmissionId);

      // Routing to SRV-INTAKE-01 for WKF-001 workflow processing
      var result = await _intakeService.ProcessIntakePayloadAsync(payload);

      if (!result.IsSuccessful)
      {
         // Route for ACT-002 (Court Clerk) review if validation rules flag the payload
         return UnprocessableEntity(result.ValidationErrors);
      }

      return Accepted(new { Message = "Case successfully ingested and set to Open/Pending status.", result.TraceabilityId });
   }
}

