using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.Services.Url;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class UrlReportController : ControllerBase
    {

        private readonly IUrlService _urlService;

        public UrlReportController(IUrlService urlService)
        {
            _urlService = urlService;
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetAllUrlReport(CancellationToken ct)
        {
            var result = await _urlService.GetAllUrlReportAsync(ct);
            return Ok(result.Value);
        }

        [HttpGet("{Id:int}")]
        public async Task<IActionResult> GetUrlReportById(int Id, CancellationToken ct)
        {
            var result = await _urlService.GetUrlReportByIdAsync(Id, ct);
            if(result.Reasons == Result.ResultEnum.DoesNotExist)
            {
                return NotFound(Id);
            }
            return Ok(result.Value);
        }
    }
}
