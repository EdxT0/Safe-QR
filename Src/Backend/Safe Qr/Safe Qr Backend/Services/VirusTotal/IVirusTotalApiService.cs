using Safe_Qr_Backend.DTO;

namespace Safe_Qr_Backend.Services.VirusTotal
{
    public interface IVirusTotalApiService
    {

        Task<AllServiceResult> EvaluateUrl(string url);
    }
}
