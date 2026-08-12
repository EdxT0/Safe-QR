using Microsoft.EntityFrameworkCore;
using Npgsql;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.DTO.UrlDTO;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.UrlReports
{
    public class UrlReportRepository : IUrlReportRepository
    {

        private readonly AppDbContext _appDbContext;

        public UrlReportRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }


        public async Task<Result<UrlReport>> AddUrlAsync(string url, AggregatedFinalResult aggregatedFinalResult, CancellationToken ct)
        {
            bool IsUniqueConstraintViolation(DbUpdateException ex)
            {
                if (ex.InnerException is not PostgresException postgresException)
                {
                    return false;
                }

                bool isUniqueViolation = postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
                return isUniqueViolation;
            }

            var urlReport = new UrlReport
            {
                Url = url,
                Results = aggregatedFinalResult,
                FlaggedForWrong = false,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _appDbContext.UrlReport.Add(urlReport);

            try
            {
                await _appDbContext.SaveChangesAsync(ct);
            }catch(DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _appDbContext.Entry(urlReport).State = EntityState.Detached;
                return Result<UrlReport>.Failure(ResultEnum.Duplicate);
            }

            return Result<UrlReport>.Succeeded(urlReport, ResultEnum.Successful);
        }

        public async Task<Result<UrlReport>> UpdateUrlAsync(string url, AggregatedFinalResult aggregatedFinalResult, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FirstOrDefaultAsync(u => u.Url == url, ct);
            if (existing != null)
            {
                existing.Results = aggregatedFinalResult;
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                try
                {
                    await _appDbContext.SaveChangesAsync(ct);
                    return Result<UrlReport>.Succeeded(existing, ResultEnum.Successful);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result<UrlReport>.Failure(ResultEnum.Failed);
                }
            }
            return Result<UrlReport>.Failure(ResultEnum.DoesNotExist);

        }

        public async Task<Result<UrlReport>> DeleteUrlAsync(int id, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FindAsync(id, ct);
            if (existing != null)
            {
                _appDbContext.UrlReport.Remove(existing);
                try
                {
                    await _appDbContext.SaveChangesAsync(ct);
                    return Result<UrlReport>.Succeeded(existing, ResultEnum.Successful);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result<UrlReport>.Failure(ResultEnum.Conflict);
                }
            }
            return Result<UrlReport>.Failure(ResultEnum.DoesNotExist); 
        }

        public async Task<Result<UrlReport>> FlagOrUnflagUrlAsync(int id, bool flag, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FindAsync(id, ct); ;
            if (existing != null)
            {
                existing.FlaggedForWrong = flag;
                try
                {
                    await _appDbContext.SaveChangesAsync(ct);
                    return Result<UrlReport>.Succeeded(existing, ResultEnum.Successful);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result<UrlReport>.Failure(ResultEnum.Conflict);
                }

            }
            return Result<UrlReport>.Failure(ResultEnum.DoesNotExist); 
        }

        public async Task<Result<UrlThreatsAnalyticsDTO>> GetThreatsAnalyticsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            var result = await _appDbContext.UrlReport.Where(u => u.UpdatedAt > from && u.UpdatedAt < to)
                                                    .GroupBy(u => new { Date = u.UpdatedAt.Date, u.Results.ServiceResultEnum })
                                                    .Select(g => new UrlThreatBucketDTO
                                                    {
                                                        Date = g.Key.Date,
                                                        Result = g.Key.ServiceResultEnum,
                                                        Count = g.Count()
                                                    }).ToListAsync(ct);
             var urlThreatsAnalytics = new UrlThreatsAnalyticsDTO
            {
                DailyBreakdowns = result,
                MaliciousCount = result.Where(r => r.Result == ServiceResultEnum.malicious).Sum(r => r.Count),
                TotalScanned = result.Sum(r => r.Count)
            };

            return Result<UrlThreatsAnalyticsDTO>.Succeeded(urlThreatsAnalytics, ResultEnum.Successful);
        }

        public async Task<Result<List<ServiceScanResult>>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FirstOrDefaultAsync(u => u.Url == url, ct);
            if (existing != null)
            {
                return Result<List<ServiceScanResult>>.Succeeded( existing.Results.ServiceScanResult , ResultEnum.Successful);
            }
            return Result<List<ServiceScanResult>>.Failure(ResultEnum.DoesNotExist) ;
        }

        public async Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct)
        {
            var urlReportList = await _appDbContext.UrlReport
                .AsNoTracking()
                .OrderByDescending(u => u.UpdatedAt)
                .ToListAsync(ct);
            return Result<List<UrlReport>>.Succeeded(urlReportList, ResultEnum.Successful);
        }

        public async Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct)
        {
            var urlReportList = await _appDbContext.UrlReport.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            if(urlReportList != null)
            {
                return Result<UrlReport>.Succeeded(urlReportList, ResultEnum.Successful);

            }
            else
            {
                return Result<UrlReport>.Failure(ResultEnum.DoesNotExist);
            }
        }


        public async Task<UrlReport?> FindByIdAsync(int id, CancellationToken ct)
        {
            return await _appDbContext.UrlReport.FindAsync(id, ct);
        
        }
    }
}

