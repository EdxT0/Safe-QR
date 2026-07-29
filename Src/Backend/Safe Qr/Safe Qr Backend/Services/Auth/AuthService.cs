using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.Repository.Users;
using System.Security.Claims;

namespace Safe_Qr_Backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;

        public AuthService(PasswordHasher<User> hasher, IUserRepository userRepository)
        {
            _passwordHasher = hasher;
            _userRepository = userRepository;
        }

        public async Task<ClaimsPrincipal?> Login(string email, string password, CancellationToken ct)
        {

            //check if user exist
            var existing = await _userRepository.GetUserByEmailAsync(email, ct);
            if (existing == null)
            {
                return null;
            }
            //verify password
            var passwordVerification = _passwordHasher.VerifyHashedPassword(existing, existing.HashedPassword, password);
            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            //claims logic
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, existing.Id.ToString()),
                new Claim(ClaimTypes.Name, existing.Name),
                new Claim(ClaimTypes.Email, existing.Email),
                new Claim(ClaimTypes.Role, existing.Role.ToString())


            };
            var identity = new ClaimsIdentity(claims, "LoginCookie");
            var principal = new ClaimsPrincipal(identity);


            return principal;



        }

    }
}
