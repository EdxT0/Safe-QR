using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.VirusTotal
{
    public interface IVirusTotalApiService
    {

        Task<ServiceScanResult> EvaluateUrlAsync(string url, CancellationToken ct);
    }
}
