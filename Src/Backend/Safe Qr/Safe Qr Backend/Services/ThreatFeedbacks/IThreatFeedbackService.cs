using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.ThreatFeedbacks
{
    public interface IThreatFeedbackService
    {
        Task<Result<ThreatFeedback>> SubmitAsync(
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
