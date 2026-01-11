using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.DTOs.Requests.User;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.UserInterfaces
{
    public interface IUserService
    {
        // Admin CRUD
        Task<IEnumerable<UserResponse>> GetAllUsersAsync(int page);
        Task<int> GetUserCountAsync();
        Task<UserResponse?> GetUserByIdAsync(int id);
        Task UpdateUserAsync(int id, UpdateUserDto updatedUser);
        Task DeleteUserAsync(int id);

        // Profile actions
        Task<User?> GetOwnProfileAsync(int id);
        Task UpdateOwnProfileAsync(int id, UserUpdateDto dto);
        Task DeleteOwnAccountAsync(int id, UserDeleteDto dto);
    }
}

// M.B