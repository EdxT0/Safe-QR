using Safe_Qr_Backend.DTO.UserController;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Services.Users
{
    public interface IUserService
    {

        Task<Result<User>> CreateUserAsync(UserCreateDTO userCreateDTO, CancellationToken ct);
    }
}
