using Safe_Qr_Backend.DTO;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.Url
{
    public interface IUrlService
    {
        Task<AggregatedFinalResult> EvaluateUrlAsync(String url, CancellationToken ct);
        Task<Result<UrlReport>> FlagUrlAsync(int id, CancellationToken ct);

        Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct);

        Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct);
    }
}
