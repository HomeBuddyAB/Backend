using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.DTOs.Requests.User;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Interfaces.UserInterfaces;
using HomeBuddy_API.Models;
using System.Security.Cryptography;
using System.Text;

namespace HomeBuddy_API.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // Admin functions
        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(int page) => await _userRepository.GetAllAsync(page);

        public async Task<int> GetUserCountAsync() => await _userRepository.GetUserCountAsync();

        public async Task<UserResponse?> GetUserByIdAsync(int id) => await _userRepository.GetByIdAsync(id);

        public async Task UpdateUserAsync(int id, UpdateUserDto updatedUser)
        {
            var existing = await _userRepository.GetByIdFullAsync(id);
            if (existing == null) throw new Exception("User not found");

            if (updatedUser.Email != null)
            {
                existing.Email = updatedUser.Email;
            }
            if (updatedUser.NewPassword != null)
            {
                CreatePasswordHash(updatedUser.NewPassword, out var newHash, out var newSalt);
                existing.PasswordHash = newHash;
                existing.PasswordSalt = newSalt;
            }

            await _userRepository.UpdateAsync(existing);
        }

        public async Task DeleteUserAsync(int id)
        {
            var existing = await _userRepository.GetByIdFullAsync(id);
            if (existing == null) throw new Exception("User not found");
            await _userRepository.DeleteAsync(existing);
        }

        // profile functions
        public async Task<User?> GetOwnProfileAsync(int id) =>
            await _userRepository.GetByIdFullAsync(id);

        public async Task UpdateOwnProfileAsync(int id, UserUpdateDto dto)
        {
            var user = await _userRepository.GetByIdFullAsync(id);
            if (user == null) throw new Exception("User not found");

            user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword))
            {
                if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                    throw new Exception("Incorrect current password");

                CreatePasswordHash(dto.NewPassword, out var newHash, out var newSalt);
                user.PasswordHash = newHash;
                user.PasswordSalt = newSalt;
            }

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteOwnAccountAsync(int id, UserDeleteDto dto)
        {
            var user = await _userRepository.GetByIdFullAsync(id);
            if (user == null) throw new Exception("User not found");

            if (!VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new Exception("Incorrect password");

            await _userRepository.DeleteAsync(user);
        }

        // verification and hashing
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