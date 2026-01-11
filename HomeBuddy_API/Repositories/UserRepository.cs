using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Interfaces.UserInterfaces;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserResponse>> GetAllAsync(int page) =>
            await _context.Users
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Cart = u.Cart
                })
                .Paginate(page)
                .ToListAsync();

        public async Task<int> GetUserCountAsync() =>
            await _context.Users.CountAsync();

        public async Task<UserResponse?> GetByIdAsync(int id) =>
            await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Cart = u.Cart
                })
                .FirstOrDefaultAsync();

        public async Task<User?> GetByIdFullAsync(int id) =>
            await _context.Users.FindAsync(id);

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}

// M.B
