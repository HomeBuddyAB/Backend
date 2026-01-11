using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Interfaces.AdminInterfaces;
using HomeBuddy_API.Models;
using System.Security.Cryptography;
using System.Text;

namespace HomeBuddy_API.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public async Task<IEnumerable<AdminResponse>> GetAllAdminsAsync(int page) =>
            await _adminRepository.GetAllAsync(page);

        public async Task<int> GetAdminCountAsync() =>
            await _adminRepository.GetAdminCountAsync();

        public async Task<AdminResponse?> GetAdminByIdAsync(int id) =>
            await _adminRepository.GetByIdAsync(id);

        public async Task<Admin?> GetAdminByIdFullAsync(int id) =>
            await _adminRepository.GetByIdFullAsync(id);

        public async Task CreateAdminAsync(string username, string password)
        {
            if (await _adminRepository.GetByUserNameAsync(username) != null)
                throw new Exception("Username already exists");

            CreatePasswordHash(password, out string hash, out string salt);

            var admin = new Admin
            {
                UserName = username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            await _adminRepository.AddAsync(admin);
        }

        public async Task UpdateAdminPasswordAsync(int id, string currentPassword, string newPassword)
        {
            var admin = await _adminRepository.GetByIdFullAsync(id);
            if (admin == null) throw new Exception("Admin not found");

            if (!VerifyPassword(currentPassword, admin.PasswordHash, admin.PasswordSalt))
                throw new Exception("Incorrect current password");

            CreatePasswordHash(newPassword, out string hash, out string salt);
            admin.PasswordHash = hash;
            admin.PasswordSalt = salt;
            await _adminRepository.UpdateAsync(admin);
        }

        public async Task DeleteAdminAsync(int id, string confirmPassword)
        {
            var admin = await _adminRepository.GetByIdFullAsync(id);
            if (admin == null) throw new Exception("Admin not found");

            if (!VerifyPassword(confirmPassword, admin.PasswordHash, admin.PasswordSalt))
                throw new Exception("Incorrect password");

            await _adminRepository.DeleteAsync(admin);
        }

        private void CreatePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(computed) == storedHash;
        }
    }
}

// M.B