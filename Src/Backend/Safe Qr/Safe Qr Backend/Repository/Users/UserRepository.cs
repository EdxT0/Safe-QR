using Microsoft.EntityFrameworkCore;
using Npgsql;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Repository.Repository.UserRepo
{
    public class UserRepository
    {

        private readonly AppDbContext _appDbContext;

        public UserRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<User>> CreateUserAsync(User user, CancellationToken ct)
        {

            bool DBErrorIsUniqueConstraint(DbUpdateException ex)
            {
                if(ex.InnerException is not PostgresException pgEx)
                {
                    return false;
                }
                return pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
            }

             _appDbContext.User.Add(user);

            try
            {
                await _appDbContext.SaveChangesAsync(ct);
                return Result<User>.Succeeded(user, ResultEnum.Successful);
            }catch(DbUpdateException ex) when (DBErrorIsUniqueConstraint(ex))
            {
                return Result<User>.Failure(ResultEnum.Duplicate);
            }


        }

        public async Task<Result<User>> UpdateUserAsync(int id, string name, string email, CancellationToken ct)
        {
            var existing = await GetExistingAsync(id, ct);
            if(existing == null)
            {
                return Result<User>.Failure(ResultEnum.DoesNotExist);
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                existing.Name = name;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                existing.Email = email;
            }

            try
            {
                await _appDbContext.SaveChangesAsync(ct);
                return Result<User>.Succeeded(existing, ResultEnum.Successful);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<User>.Failure(ResultEnum.Conflict);
            }
        }

        public async Task<Result<User>> SetUserEnabledAsync(int id, bool isEnabled, CancellationToken ct)
        {
            var existing = await GetExistingAsync(id, ct);
            if (existing == null)
            {
                return Result<User>.Failure(ResultEnum.DoesNotExist);
            }
            existing.Enabled = isEnabled;

            try
            {
                await _appDbContext.SaveChangesAsync(ct);
                return Result<User>.Succeeded(existing, ResultEnum.Successful);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<User>.Failure(ResultEnum.Conflict);
            }
        
        }

        public async Task<List<User>> GetAllUserAsync(CancellationToken ct)
        {
            return await _appDbContext.User.AsNoTracking().ToListAsync(ct);
        }

        public async Task<User?> GetUserByUsernameAsync(String name, CancellationToken ct)
        {
            return await _appDbContext.User.AsNoTracking().FirstOrDefaultAsync(u => u.Name == name, ct);
        }

        public async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct)
        {
            return await _appDbContext.User.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        private async Task<User?> GetExistingAsync(int id, CancellationToken ct)
        {
            return await _appDbContext.User.FindAsync(id, ct);
        }
    }
}
