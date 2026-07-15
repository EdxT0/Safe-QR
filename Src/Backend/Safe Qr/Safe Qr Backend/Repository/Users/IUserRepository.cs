using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Repository.Repository.Users
{
    public interface IUserRepository
    {


        Task<Result<User>> CreateUserAsync(User user, CancellationToken ct);

        Task<Result<User>> UpdateUserAsync(int id, string name, string email, CancellationToken ct);

        Task<Result<User>> SetUserEnabledAsync(int id,  bool isEnabled, CancellationToken ct);

        Task<List<User>> GetAllUserAsync(CancellationToken ct);
        Task<User?> GetUserByEmailAsync(string email, CancellationToken ct);
        Task<User?> GetUserByUsernameAsync(String name, CancellationToken ct);
    }
}
