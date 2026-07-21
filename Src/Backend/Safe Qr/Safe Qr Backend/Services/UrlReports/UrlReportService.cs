using Safe_Qr_Backend.DTO.UrlDTO;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.UrlReports
{
    public class UrlReportService: IUrlReportService
    {
        private readonly IUrlReportRepository _urlReportRepository;
        public UrlReportService(IUrlReportRepository urlReportRepository)
        {
            _urlReportRepository = urlReportRepository;
        }

        public async Task<UrlThreatsAnalyticsDTO> GetThreatsAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken ct)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var toUtc = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            var result =  await _urlReportRepository.GetThreatsAnalyticsAsync(fromUtc, toUtc, ct);
            return result.Value!;
        }

        public async Task<Result<UrlReport>> FlagUrlAsync(int id, CancellationToken ct)
        {
            var result = await _urlReportRepository.FlagOrUnflagUrlAsync(id, true, ct);
            return result;
        }
        public async Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct)
        {
            return await _urlReportRepository.GetUrlReportByIdAsync(id, ct);
        }
        public async Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct)
        {
            var result = await _urlReportRepository.GetAllUrlReportAsync(ct);
            return result;
        }
    }
}
