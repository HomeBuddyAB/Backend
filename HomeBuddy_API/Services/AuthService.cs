using HomeBuddy_API.DTOs.Requests.Auth;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HomeBuddy_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _config;

        public AuthService(IAuthRepository authRepo, IConfiguration config)
        {
            _authRepo = authRepo;
            _config = config;
        }

        // User Registration
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("Email already registered");

            CreatePasswordHash(dto.Password, out string hash, out string salt);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            await _authRepo.AddUserAsync(user);
            await _authRepo.SaveChangesAsync();

            return new AuthResponseDto
            {
                Email = user.Email,
                Token = GenerateJwtToken(user.Email, user.Id, "User")
            };
        }

        // User Login
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new Exception("Invalid credentials");

            return new AuthResponseDto
            {
                Email = user.Email,
                Token = GenerateJwtToken(user.Email, user.Id, "User")
            };
        }

        // Admin Login
        public async Task<AuthResponseDto> LoginAdminAsync(AdminLoginDto dto)
        {
            var admin = await _authRepo.GetAdminByUserNameAsync(dto.UserName);
            if (admin == null || !VerifyPassword(dto.Password, admin.PasswordHash, admin.PasswordSalt))
                throw new Exception("Invalid admin credentials");

            return new AuthResponseDto
            {
                Email = admin.UserName,
                Token = GenerateJwtToken(admin.UserName, admin.Id, "Admin")
            };
        }
        // Helper Methods
        private void CreatePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(computedHash) == storedHash;
        }

        private string GenerateJwtToken(string email, int id, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

// M.B