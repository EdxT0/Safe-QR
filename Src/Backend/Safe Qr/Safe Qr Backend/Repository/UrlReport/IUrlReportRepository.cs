using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.UrlReportRepo
{
    public interface IUrlReportRepository
    {

        Task<RepoResult<UrlReport>> AddUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<RepoResult<UrlReport>> UpdateUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<RepoResult> DeleteUrlAsync(string url, CancellationToken ct);

        Task<RepoResult<UrlReport>> FlagOrUnflagUrlAsync(string url, bool flag, CancellationToken ct);

        Task<List<ServiceResult>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct);
    }
}
