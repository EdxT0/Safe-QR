using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.DTO.SandboxDTO;
using Safe_Qr_Backend.Services.Sandbox;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SandboxController : ControllerBase
    {
        private readonly ISandboxScreenshotService _sandboxScreenshotService;

        public SandboxController(ISandboxScreenshotService sandboxScreenshotService)
        {
            _sandboxScreenshotService = sandboxScreenshotService;
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] SandboxPreviewRequestDTO dto, CancellationToken ct)
        {
            var result = await _sandboxScreenshotService.CapturePreviewAsync(dto.Url, ct);
            if (!result.IsSucceeded)
            {
                return BadRequest("Could not render a preview of this URL.");
            }

            return Ok(new SandboxPreviewResponseDTO(Convert.ToBase64String(result.Value!)));
        }
    }
}
