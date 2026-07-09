using System.Security.Claims;

namespace Safe_Qr_Backend.Services.Auth
{
    public interface IAuthService
    {

        Task<ClaimsPrincipal?> Login(string email, string password, CancellationToken ct);

    }
}
