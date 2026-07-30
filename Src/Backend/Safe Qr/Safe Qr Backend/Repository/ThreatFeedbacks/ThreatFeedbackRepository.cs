using Microsoft.EntityFrameworkCore;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.ThreatFeedbacks
{
    public class ThreatFeedbackRepository : IThreatFeedbackRepository
    {
        private readonly AppDbContext _appDbContext;

        public ThreatFeedbackRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<ThreatFeedback>> AddAsync(
            int? userId,
            string payload,
            string payloadType,
            AggregatedFinalResult systemClassification,
            ServiceResultEnum reportedRiskLevel,
            string? comment,
            CancellationToken ct)
        {
            var feedback = new ThreatFeedback
            {
                UserId = userId,
                Payload = payload,
                PayloadType = payloadType,
                SystemClassification = systemClassification,
                ReportedRiskLevel = reportedRiskLevel,
                Comment = comment,
            };

            _appDbContext.ThreatFeedback.Add(feedback);
            await _appDbContext.SaveChangesAsync(ct);

            return Result<ThreatFeedback>.Succeeded(feedback, ResultEnum.Successful);
        }

        public async Task<Result<List<ThreatFeedback>>> GetAllAsync(CancellationToken ct)
        {
            var feedback = await _appDbContext.ThreatFeedback
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(ct);

            return Result<List<ThreatFeedback>>.Succeeded(feedback, ResultEnum.Successful);
        }
    }
}
