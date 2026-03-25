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
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
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
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.NewPassword);
                existing.PasswordSalt = string.Empty;
            }

            await _userRepository.UpdateAsync(existing);
            _logger.LogInformation("Admin updated user {UserId}. Email changed={EmailChanged}, Password changed={PasswordChanged}",
                id,
                updatedUser.Email != null,
                updatedUser.NewPassword != null);
        }

        public async Task DeleteUserAsync(int id)
        {
            var existing = await _userRepository.GetByIdFullAsync(id);
            if (existing == null) throw new Exception("User not found");
            await _userRepository.DeleteAsync(existing);
            _logger.LogInformation("Admin deleted user {UserId}", id);
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

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.PasswordSalt = string.Empty;
            }

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("User {UserId} updated own profile. Email changed={EmailChanged}, Password changed={PasswordChanged}",
                id,
                dto.Email != null && !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase),
                !string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword));
        }

        public async Task DeleteOwnAccountAsync(int id, UserDeleteDto dto)
        {
            var user = await _userRepository.GetByIdFullAsync(id);
            if (user == null) throw new Exception("User not found");

            if (!VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new Exception("Incorrect password");

            await _userRepository.DeleteAsync(user);
            _logger.LogInformation("User {UserId} deleted own account", id);
        }

        // verification and hashing
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            // Legacy hashes (HMACSHA512) store a non-empty salt.
            if (!string.IsNullOrWhiteSpace(storedSalt))
            {
                using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
                var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(computed) == storedHash;
            }

            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}

// M.B