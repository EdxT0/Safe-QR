using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.Services.UrlReports;
using Safe_Qr_Backend.Services.UrlScans;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UrlReportController : ControllerBase
    {

        private readonly IUrlReportService _urlReportService;


        public UrlReportController(IUrlReportService urlReportService)
        {
            _urlReportService = urlReportService;
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetAllUrlReport(CancellationToken ct)
        {
            var result = await _urlReportService.GetAllUrlReportAsync(ct);
            return Ok(result.Value);
        }

        [HttpGet("{Id:int}")]
        public async Task<IActionResult> GetUrlReportById(int Id, CancellationToken ct)
        {
            var result = await _urlReportService.GetUrlReportByIdAsync(Id, ct);
            if(result.Reasons == Result.ResultEnum.DoesNotExist)
            {
                return NotFound(Id);
            }
            return Ok(result.Value);
        }

        [HttpGet("analytics/threats")]
        public async Task<IActionResult> GetThreatsAnalytics([FromQuery]DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var effectiveFrom = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var effectiveTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var result = await _urlReportService.GetThreatsAnalyticsAsync(effectiveFrom, effectiveTo, ct);
            return Ok(result);
        }
    }
}
