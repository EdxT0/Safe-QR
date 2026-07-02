using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.UrlReportRepo
{
    public interface IUrlReportRepository
    {

        Task<bool> AddUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<bool> UpdateUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct);

        Task<bool> DeleteUrlAsync(string url, CancellationToken ct);

        Task<bool> FlagOrUnflagUrlAsync(string url, bool flag, CancellationToken ct);

        Task<List<ServiceResult>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct);
    }
}
