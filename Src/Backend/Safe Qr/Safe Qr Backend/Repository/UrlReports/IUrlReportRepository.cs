using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.UrlReports
{
    public interface IUrlReportRepository
    {

        Task<Result<UrlReport>> AddUrlAsync(string url, AggregatedFinalResult aggregatedFinalResult, CancellationToken ct);

        Task<Result<UrlReport>> UpdateUrlAsync(string url, AggregatedFinalResult aggregatedFinalResult, CancellationToken ct);

        Task<Result<UrlReport>> DeleteUrlAsync(int id, CancellationToken ct);

        Task<Result<UrlReport>> FlagOrUnflagUrlAsync(int id, bool flag, CancellationToken ct);

        Task<Result<List<ServiceScanResult>>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct);

        Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct);
        Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct);
    }
}
