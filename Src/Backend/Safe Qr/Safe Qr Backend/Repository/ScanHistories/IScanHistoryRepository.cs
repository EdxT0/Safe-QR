using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.ScanHistories
{
    public interface IScanHistoryRepository
    {
        Task<Result<ScanHistory>> AddAsync(int userId, string payload, string payloadType, AggregatedFinalResult result, CancellationToken ct);

        Task<Result<List<ScanHistory>>> GetAllByUserAsync(int userId, CancellationToken ct);

        Task<Result<ScanHistory>> DeleteAsync(int id, int userId, CancellationToken ct);
    }
}
