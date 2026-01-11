using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.UserInterfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserResponse>> GetAllAsync(int page);
        Task<int> GetUserCountAsync();
        Task<UserResponse?> GetByIdAsync(int id);
        Task<User?> GetByIdFullAsync(int id);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task SaveChangesAsync();
    }
}

// M.B