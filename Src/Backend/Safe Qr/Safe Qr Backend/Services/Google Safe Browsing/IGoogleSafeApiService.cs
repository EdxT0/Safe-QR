using Safe_Qr_Backend.DTO;

namespace Safe_Qr_Backend.Services.Google_Safe_Browsing
{
    public interface IGoogleSafeApiService
    {
        Task<AllServiceResult> EvaluateUrl(string url);
    }
}
