using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.Google_Safe_Browsing
{
    public interface IGoogleSafeApiService
    {
        Task<ServiceScanResult> EvaluateUrl(string url, CancellationToken ct);
    }
}
