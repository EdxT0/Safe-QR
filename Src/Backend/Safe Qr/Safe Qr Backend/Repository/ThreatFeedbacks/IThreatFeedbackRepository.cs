using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.ThreatFeedbacks
{
    public interface IThreatFeedbackRepository
    {
        Task<Result<ThreatFeedback>> AddAsync(
            int? userId,
            string payload,
            string payloadType,
            AggregatedFinalResult systemClassification,
            ServiceResultEnum reportedRiskLevel,
            string? comment,
            CancellationToken ct);

        Task<Result<List<ThreatFeedback>>> GetAllAsync(CancellationToken ct);
    }
}
