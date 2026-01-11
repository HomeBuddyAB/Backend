using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Interfaces.AdminInterfaces;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AdminResponse>> GetAllAsync(int page) =>
            await _context.Admins
                .Select(a => new AdminResponse
                {
                    Id = a.Id,
                    UserName = a.UserName
                })
                .Paginate(page)
                .ToListAsync();

        public async Task<int> GetAdminCountAsync() =>
            await _context.Admins.CountAsync();

        public async Task<AdminResponse?> GetByIdAsync(int id) =>
            await _context.Admins
                .Where(a => a.Id == id)
                .Select(a => new AdminResponse
                {
                    Id = a.Id,
                    UserName = a.UserName
                })
                .FirstOrDefaultAsync();

        public async Task<Admin?> GetByIdFullAsync(int id) =>
            await _context.Admins.FindAsync(id);

        public async Task<AdminResponse?> GetByUserNameAsync(string username) =>
            await _context.Admins
                .Where(a => a.UserName == username)
                .Select(a => new AdminResponse
                {
                    Id = a.Id,
                    UserName = a.UserName
                })
                .FirstOrDefaultAsync();

        public async Task AddAsync(Admin admin)
        {
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Admin admin)
        {
            _context.Admins.Update(admin);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Admin admin)
        {
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}

// M.B