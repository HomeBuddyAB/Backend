using HomeBuddy_API.Models;
using System.Threading.Tasks;

namespace HomeBuddy_API.Interfaces.AuthInterfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<Admin?> GetAdminByUserNameAsync(string username);
        Task AddUserAsync(User user);
        Task AddAdminAsync(Admin admin);
        Task SaveChangesAsync();
    }
}

// M.B