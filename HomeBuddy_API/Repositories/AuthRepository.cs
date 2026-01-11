using HomeBuddy_API.Data;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Admin?> GetAdminByUserNameAsync(string username)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.UserName == username);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task AddAdminAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

// M.B