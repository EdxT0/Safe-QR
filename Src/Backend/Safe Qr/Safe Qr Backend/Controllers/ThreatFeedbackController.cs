using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.DTO.ThreatFeedbackDTO;
using Safe_Qr_Backend.Services.ThreatFeedbacks;
using System.Security.Claims;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThreatFeedbackController : ControllerBase
    {
        private readonly IThreatFeedbackService _threatFeedbackService;

        public ThreatFeedbackController(IThreatFeedbackService threatFeedbackService)
        {
            _threatFeedbackService = threatFeedbackService;
        }

        // Intentionally open — no [Authorize]. Reporting a misclassification shouldn't
        // require an account, matching the already-anonymous /api/Scan flow. When the
        // caller does have a valid session, we still attribute the report to them.
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitThreatFeedbackDTO dto, CancellationToken ct)
        {
            int? userId = null;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim is not null && int.TryParse(idClaim.Value, out var parsedId))
            {
                userId = parsedId;
            }

            var result = await _threatFeedbackService.SubmitAsync(
                userId, dto.Payload, dto.PayloadType, dto.SystemClassification, dto.ReportedRiskLevel, dto.Comment, ct);

            return Ok(result.Value);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _threatFeedbackService.GetAllAsync(ct);
            return Ok(result.Value);
        }
    }
}
