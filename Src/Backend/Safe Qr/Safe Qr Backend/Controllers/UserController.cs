using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                return Ok(result.Value);
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
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal, new AuthenticationProperties
            {
                IsPersistent = false
            });
            return Ok($"User {userLoginDetails.Email}, logged in");
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
