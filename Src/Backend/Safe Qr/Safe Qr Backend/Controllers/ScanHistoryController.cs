using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.DTO.ScanHistoryDTO;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.ScanHistories;
using System.Security.Claims;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ScanHistoryController : ControllerBase
    {
        private readonly IScanHistoryService _scanHistoryService;

        public ScanHistoryController(IScanHistoryService scanHistoryService)
        {
            _scanHistoryService = scanHistoryService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _scanHistoryService.GetAllForUserAsync(CurrentUserId, ct);
            return Ok(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveScanHistoryDTO dto, CancellationToken ct)
        {
            var result = await _scanHistoryService.SaveAsync(CurrentUserId, dto.Payload, dto.PayloadType, dto.Result, ct);
            return Ok(result.Value);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _scanHistoryService.DeleteAsync(id, CurrentUserId, ct);
            if (result.IsSucceeded == false && result.Reasons == ResultEnum.DoesNotExist)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
