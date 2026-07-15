using Microsoft.AspNetCore.Identity;
using Safe_Qr_Backend.DTO.UserController;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.Repository.Users;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<UserCreateDTO> _passwordHasher;
        public UserService(IUserRepository userRepository, IPasswordHasher<UserCreateDTO> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<User>> CreateUserAsync(UserCreateDTO userCreateDTO, CancellationToken ct)
        {
            UserRoleEnum roleGiven;
            switch (userCreateDTO.role.ToLower())
            {
                case "admin":
                    roleGiven = UserRoleEnum.Admin;
                    break;
                case "user":
                    roleGiven = UserRoleEnum.User;
                    break;
                case "test":
                    roleGiven = UserRoleEnum.Test;
                    break;
                default:
                    return Result<User>.Failure(ResultEnum.RoleDoesNotExist);
            };

            var userHashedPassword = _passwordHasher.HashPassword(userCreateDTO, userCreateDTO.Password);

            var user = new User() { Name = userCreateDTO.Name, Email = userCreateDTO.Email, Role = roleGiven, HashedPassword = userHashedPassword };

            return await _userRepository.CreateUserAsync(user, ct);

           
        }
    }
}
