using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.ThreatFeedbacks;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.ThreatFeedbacks
{
    public class ThreatFeedbackService : IThreatFeedbackService
    {
        private readonly IThreatFeedbackRepository _threatFeedbackRepository;

        public ThreatFeedbackService(IThreatFeedbackRepository threatFeedbackRepository)
        {
            _threatFeedbackRepository = threatFeedbackRepository;
        }

        public async Task<Result<ThreatFeedback>> SubmitAsync(
            int? userId,
            string payload,
            string payloadType,
            AggregatedFinalResult systemClassification,
            ServiceResultEnum reportedRiskLevel,
            string? comment,
            CancellationToken ct)
        {
            return await _threatFeedbackRepository.AddAsync(
                userId, payload, payloadType, systemClassification, reportedRiskLevel, comment, ct);
        }

        public async Task<Result<List<ThreatFeedback>>> GetAllAsync(CancellationToken ct)
        {
            return await _threatFeedbackRepository.GetAllAsync(ct);
        }
    }
}
