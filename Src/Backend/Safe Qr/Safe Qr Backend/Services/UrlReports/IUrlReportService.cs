using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.DTO.UrlDTO;

namespace Safe_Qr_Backend.Services.UrlReports
{
    public interface IUrlReportService
    {

        Task<Result<UrlReport>> FlagUrlAsync(int id, CancellationToken ct);

        Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct);

        Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct);
        Task<UrlThreatsAnalyticsDTO> GetThreatsAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken ct);
    }
}
