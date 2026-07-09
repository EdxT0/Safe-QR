using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.VirusTotal
{
    public interface IVirusTotalApiService
    {

        Task<ServiceScanResult> EvaluateUrl(string url, CancellationToken ct);
    }
}
