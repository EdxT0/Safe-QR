using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.UrlReportRepo
{
    public interface IUrlReportRepository
    {

        Task<Result<UrlReport>> AddUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<Result<UrlReport>> UpdateUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<Result<UrlReport>> DeleteUrlAsync(int id, CancellationToken ct);

        Task<Result<UrlReport>> FlagOrUnflagUrlAsync(int id, bool flag, CancellationToken ct);

        Task<Result<List<ServiceResult>>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct);
    }
}
