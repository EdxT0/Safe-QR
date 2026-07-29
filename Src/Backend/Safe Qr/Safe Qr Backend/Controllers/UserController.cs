using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safe_Qr_Backend.DTO.UserController;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.Users;
using Safe_Qr_Backend.Services.Auth;
using System.Security.Claims;

namespace Safe_Qr_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public UserController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDTO userCreateDTO, CancellationToken ct)
        {
            var result = await _userService.CreateUserAsync(userCreateDTO, ct);

            if (result.Reasons == ResultEnum.Duplicate)
            {
                return Conflict("User already exists");
            }
            else if (result.Reasons == ResultEnum.RoleDoesNotExist)
            {
                return BadRequest("Role does not exist");
            }
            else if (result.Reasons == ResultEnum.Successful)
            {
                var user = result.Value!;
                return Ok(new UserPublicDTO(user.Id, user.Name, user.Email, user.Role.ToString()));
            }
            else
            {
                return Problem();
            }
        }



        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDTO userLoginDetails, CancellationToken ct)
        {
            var userPrincipal = await _authService.Login(userLoginDetails.Email, userLoginDetails.Password, ct);

            if(userPrincipal == null)
            {
                return NotFound("Email/Password Incorrect");
            }
            await HttpContext.SignInAsync("LoginCookie", userPrincipal, new AuthenticationProperties
            {
                IsPersistent = false
            });

            var user = new UserPublicDTO(
                int.Parse(userPrincipal.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                userPrincipal.FindFirst(ClaimTypes.Name)!.Value,
                userPrincipal.FindFirst(ClaimTypes.Email)!.Value,
                userPrincipal.FindFirst(ClaimTypes.Role)!.Value);

            return Ok(user);
        }

        [HttpGet("Me")]
        [Authorize]
        public IActionResult Me()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null)
            {
                return Unauthorized();
            }

            var user = new UserPublicDTO(
                int.Parse(idClaim.Value),
                User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty);

            return Ok(user);
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> LogoutAsync()
        {
            var user = User.FindFirst(ClaimTypes.Name);
            if(user == null)
            {
                return NotFound("No User Logged in");
            }
            await HttpContext.SignOutAsync();
            return Ok($"User {user.Value}, logged out");
        }


    }
}
