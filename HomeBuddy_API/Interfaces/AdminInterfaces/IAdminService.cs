using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.AdminInterfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<AdminResponse>> GetAllAdminsAsync(int page);
        Task<int> GetAdminCountAsync();
        Task<AdminResponse?> GetAdminByIdAsync(int id);
        Task<Admin?> GetAdminByIdFullAsync(int id);
        Task CreateAdminAsync(string username, string password);
        Task UpdateAdminPasswordAsync(int id, string currentPassword, string newPassword);
        Task DeleteAdminAsync(int id, string confirmPassword);
    }
}

// M.B