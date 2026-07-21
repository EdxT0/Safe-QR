using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.DTO.UrlDTO;
using Safe_Qr_Backend.Services.UrlScans;
using Safe_Qr_Backend.Services.UrlReports;


namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScanController : ControllerBase
    {
        private readonly IUrlScanService _urlScanService;
        private readonly IUrlReportService _urlReportService;


        public ScanController(IUrlScanService urlScanService, IUrlReportService urlReportService)
        {
            _urlScanService = urlScanService;
            _urlReportService = urlReportService;
        }

        [HttpPost]
        public async Task<IActionResult> ScanUrlAsync([FromBody] ScanUrlDTO scanUrlDTO, CancellationToken ct)
        {
            var scanResult=  await _urlScanService.PipelineEvaluate(scanUrlDTO.Url, ct);

            return Ok(scanResult);
        }

        [HttpPatch("{id:int}/flag")]
        public async Task<IActionResult> SetUrlFlagAsync([FromQuery] int id, CancellationToken ct)
        {
            var result = await _urlReportService.FlagUrlAsync(id, ct);
            
               
            if(result.IsSucceeded == true)
            {
                return Ok(result.Value!.Results);
            }

            if(result.IsSucceeded == false && result.Reasons == Result.ResultEnum.Conflict)
            {
                return Conflict();
            }
            if(result.IsSucceeded == false && result.Reasons == Result.ResultEnum.DoesNotExist)
            {
                return NotFound();
            }
            return Problem();
 
        }
    }
}
