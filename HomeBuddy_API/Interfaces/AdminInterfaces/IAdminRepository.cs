using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.AdminInterfaces
{
    public interface IAdminRepository
    {
        Task<IEnumerable<AdminResponse>> GetAllAsync(int page);
        Task<int> GetAdminCountAsync();
        Task<AdminResponse?> GetByIdAsync(int id);
        Task<Admin?> GetByIdFullAsync(int id);
        Task<AdminResponse?> GetByUserNameAsync(string username);
        Task AddAsync(Admin admin);
        Task UpdateAsync(Admin admin);
        Task DeleteAsync(Admin admin);
        Task SaveChangesAsync();
    }
}

// M.B