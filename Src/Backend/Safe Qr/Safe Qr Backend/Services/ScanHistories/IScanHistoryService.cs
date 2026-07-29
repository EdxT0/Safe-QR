using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.ScanHistories
{
    public interface IScanHistoryService
    {
        Task<Result<ScanHistory>> SaveAsync(int userId, string payload, string payloadType, AggregatedFinalResult result, CancellationToken ct);

        Task<Result<List<ScanHistory>>> GetAllForUserAsync(int userId, CancellationToken ct);

        Task<Result<ScanHistory>> DeleteAsync(int id, int userId, CancellationToken ct);
    }
}
