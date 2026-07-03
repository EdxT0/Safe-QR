using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.Repository.UserRepo
{
    public interface IUserRepository
    {


        Task<RepoResult<User>> CreateUserAsync(string name, string hashedPassword, CancellationToken ct);

        Task<RepoResult<User>> UpdateUserAsync(string name, string email, CancellationToken ct);

        Task<RepoResult<User>> SetUserEnabledAsync(string name, bool isEnabled, CancellationToken ct);

        Task<List<User>> GetUserAsync(CancellationToken ct);

        Task<User> GetUserByUsernameAsync(String name, CancellationToken ct);

    }
}
