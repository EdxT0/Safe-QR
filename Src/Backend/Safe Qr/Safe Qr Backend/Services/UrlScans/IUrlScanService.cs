using Safe_Qr_Backend.DTO;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.UrlScans
{
    public interface IUrlScanService
    {
        Task<AggregatedFinalResult> PipelineEvaluate(string url, CancellationToken ct = default);

    }
}
