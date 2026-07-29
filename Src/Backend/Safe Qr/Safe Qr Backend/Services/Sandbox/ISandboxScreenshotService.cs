using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.Sandbox
{
    public interface ISandboxScreenshotService
    {
        Task<Result<byte[]>> CapturePreviewAsync(string url, CancellationToken ct);
    }
}
