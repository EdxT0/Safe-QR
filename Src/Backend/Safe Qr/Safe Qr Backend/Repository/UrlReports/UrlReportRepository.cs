using Microsoft.EntityFrameworkCore;
using Npgsql;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.UrlReportRepo
{
    public class UrlReportRepository : IUrlReportRepository
    {

        private readonly AppDbContext _appDbContext;

        public UrlReportRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }


        public async Task<Result<UrlReport>> AddUrlAsync(string url, List<ServiceResult> serviceResults, CancellationToken ct)
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
                Results = serviceResults,
                FlaggedForWrong = false
            };
            _appDbContext.UrlReport.Add(urlReport);

            try
            {
                await _appDbContext.SaveChangesAsync(ct);
            }catch(DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Result<UrlReport>.Failure(ResultEnum.Duplicate);
            }

            return Result<UrlReport>.Succeeded(urlReport, ResultEnum.Successful);
        }

        public async Task<Result<UrlReport>> UpdateUrlAsync(string url, List<ServiceResult> serviceResults, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FirstOrDefaultAsync(u => u.Url == url, ct);
            if (existing != null)
            {
                existing.Results = serviceResults;

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

        public async Task<Result<List<ServiceResult>>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FirstOrDefaultAsync(u => u.Url == url, ct);
            if (existing != null)
            {
                return Result<List<ServiceResult>>.Succeeded( existing.Results, ResultEnum.Successful);
            }
            return Result<List<ServiceResult>>.Failure(ResultEnum.DoesNotExist) ;
        }

        public async Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct)
        {
            var urlReportList = await _appDbContext.UrlReport.AsNoTracking().ToListAsync(ct);
            return Result<List<UrlReport>>.Succeeded(urlReportList, ResultEnum.Successful);
        }


        public async Task<UrlReport?> FindByIdAsync(int id, CancellationToken ct)
        {
            return await _appDbContext.UrlReport.FindAsync(id, ct);
        
        }
    }
}

