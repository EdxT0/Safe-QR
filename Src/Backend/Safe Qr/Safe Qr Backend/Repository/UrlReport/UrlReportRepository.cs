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


        public async Task<RepoResult<UrlReport>> AddUrlAsync(string url, List<ServiceResult> serviceResults, CancellationToken ct)
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
                return RepoResult<UrlReport>.Failure(RepoResultEnum.Duplicate);
            }


            


            return RepoResult<UrlReport>.Succeeded(urlReport, RepoResultEnum.Successful);
        }

        public async Task<RepoResult<UrlReport>> UpdateUrlAsync(string url, List<ServiceResult> ServiceResults, CancellationToken ct)
        {
            var existing = await FindByUrlAsync(url, ct);
            if (existing != null)
            {
                existing.Results = ServiceResults;

                await _appDbContext.SaveChangesAsync(ct);
                return RepoResult<UrlReport>.Succeeded(existing, RepoResultEnum.Successful);
            }
            return RepoResult<UrlReport>.Failure(RepoResultEnum.Failed); ;

        }

        public async Task<RepoResult> DeleteUrlAsync(string url, CancellationToken ct)
        {
            var existing = await FindByUrlAsync(url, ct);
            if (existing != null)
            {
                _appDbContext.UrlReport.Remove(existing);
                await _appDbContext.SaveChangesAsync(ct);
                return RepoResult<UrlReport>.Succeeded(RepoResultEnum.Successful); ;
            }
            return RepoResult<UrlReport>.Failure(RepoResultEnum.Failed); ;
        }

        public async Task<RepoResult<UrlReport>> FlagOrUnflagUrlAsync(string url, bool flag, CancellationToken ct)
        {
            var existing = await FindByUrlAsync(url, ct);
            if (existing != null)
            {
                existing.FlaggedForWrong = flag;
                await _appDbContext.SaveChangesAsync(ct);
                return RepoResult<UrlReport>.Succeeded(existing, RepoResultEnum.Successful); ;
            }
            return RepoResult<UrlReport>.Failure(RepoResultEnum.Failed); ;
        }

        public async Task<List<ServiceResult>> GetAllServiceResultByUrlAsync(string url, CancellationToken ct)
        {
            var existing = await FindByUrlAsync(url, ct);
            if (existing != null)
            {
                return existing.Results;
            }
            return new List<ServiceResult>();
        }

        private async Task<UrlReport?> FindByUrlAsync(string url, CancellationToken ct)
        {
            var existing = await _appDbContext.UrlReport.FindAsync(url, ct);
            if (existing != null)
            {
                return existing;
            }
            return null;
        }
    }
}

