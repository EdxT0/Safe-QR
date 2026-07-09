using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.Repository.UserRepo
{
    public interface IUserRepository
    {


        Task<Result<User>> CreateUserAsync(User user, CancellationToken ct);

        Task<Result<User>> UpdateUserAsync(string name, string email, CancellationToken ct);

        Task<Result<User>> SetUserEnabledAsync(string name, bool isEnabled, CancellationToken ct);

        Task<List<User>> GetUserAsync(CancellationToken ct);
        Task<User?> GetUserByEmailAsync(string email, CancellationToken ct);
        Task<User> GetUserByUsernameAsync(String name, CancellationToken ct);

    }
}
