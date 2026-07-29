using Microsoft.EntityFrameworkCore;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.ScanHistories
{
    public class ScanHistoryRepository : IScanHistoryRepository
    {
        private readonly AppDbContext _appDbContext;

        public ScanHistoryRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<ScanHistory>> AddAsync(int userId, string payload, string payloadType, AggregatedFinalResult result, CancellationToken ct)
        {
            var scanHistory = new ScanHistory
            {
                UserId = userId,
                Payload = payload,
                PayloadType = payloadType,
                Results = result
            };

            _appDbContext.ScanHistory.Add(scanHistory);
            await _appDbContext.SaveChangesAsync(ct);

            return Result<ScanHistory>.Succeeded(scanHistory, ResultEnum.Successful);
        }

        public async Task<Result<List<ScanHistory>>> GetAllByUserAsync(int userId, CancellationToken ct)
        {
            var records = await _appDbContext.ScanHistory
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ScannedAt)
                .ToListAsync(ct);

            return Result<List<ScanHistory>>.Succeeded(records, ResultEnum.Successful);
        }

        public async Task<Result<ScanHistory>> DeleteAsync(int id, int userId, CancellationToken ct)
        {
            var existing = await _appDbContext.ScanHistory.FindAsync(new object?[] { id }, ct);
            if (existing == null)
            {
                return Result<ScanHistory>.Failure(ResultEnum.DoesNotExist);
            }

            if (existing.UserId != userId)
            {
                return Result<ScanHistory>.Failure(ResultEnum.DoesNotExist);
            }

            _appDbContext.ScanHistory.Remove(existing);
            await _appDbContext.SaveChangesAsync(ct);

            return Result<ScanHistory>.Succeeded(existing, ResultEnum.Successful);
        }
    }
}
