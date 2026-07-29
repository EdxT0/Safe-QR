using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.ScanHistories;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.ScanHistories
{
    public class ScanHistoryService : IScanHistoryService
    {
        private readonly IScanHistoryRepository _scanHistoryRepository;

        public ScanHistoryService(IScanHistoryRepository scanHistoryRepository)
        {
            _scanHistoryRepository = scanHistoryRepository;
        }

        public async Task<Result<ScanHistory>> SaveAsync(int userId, string payload, string payloadType, AggregatedFinalResult result, CancellationToken ct)
        {
            return await _scanHistoryRepository.AddAsync(userId, payload, payloadType, result, ct);
        }

        public async Task<Result<List<ScanHistory>>> GetAllForUserAsync(int userId, CancellationToken ct)
        {
            return await _scanHistoryRepository.GetAllByUserAsync(userId, ct);
        }

        public async Task<Result<ScanHistory>> DeleteAsync(int id, int userId, CancellationToken ct)
        {
            return await _scanHistoryRepository.DeleteAsync(id, userId, ct);
        }
    }
}
