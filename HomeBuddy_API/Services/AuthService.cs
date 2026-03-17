using HomeBuddy_API.DTOs.Requests.Auth;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Interfaces.EmailInterfaces;
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
        private readonly IEmailSender _emailSender;

        public AuthService(IAuthRepository authRepo, IConfiguration config, IEmailSender emailSender)
        {
            _authRepo = authRepo;
            _config = config;
            _emailSender = emailSender;
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

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            // Always respond OK to avoid user enumeration
            var user = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (user == null)
            {
                return;
            }

            var token = GenerateResetToken();
            var tokenHash = HashToken(token);

            user.PasswordResetTokenHash = tokenHash;
            user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(GetResetTokenTtlMinutes());
            await _authRepo.SaveChangesAsync();

            var resetLink = BuildResetLink(token);
            var subject = "Reset your HomeBuddy password";
            var body = $@"
<div style=""font-family:Arial,sans-serif;line-height:1.5"">
  <h2>Reset your password</h2>
  <p>We received a request to reset your password. Click the button below to choose a new one.</p>
  <p style=""margin:24px 0"">
    <a href=""{resetLink}"" style=""background:#8B4545;color:#fff;padding:12px 18px;text-decoration:none;border-radius:6px;display:inline-block"">Reset password</a>
  </p>
  <p>If you didn’t request this, you can ignore this email.</p>
  <p style=""color:#666;font-size:12px"">This link expires in {GetResetTokenTtlMinutes()} minutes.</p>
</div>";

            await _emailSender.SendAsync(user.Email, subject, body);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var tokenHash = HashToken(dto.Token);
            var user = await _authRepo.GetUserByPasswordResetTokenHashAsync(tokenHash);

            if (user == null ||
                user.PasswordResetTokenExpiresAtUtc == null ||
                user.PasswordResetTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired reset token");
            }

            CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAtUtc = null;

            await _authRepo.SaveChangesAsync();
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

        private string GenerateResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private int GetResetTokenTtlMinutes()
        {
            var ttlStr = _config["PasswordReset:TokenTtlMinutes"];
            if (int.TryParse(ttlStr, out var ttl) && ttl >= 5 && ttl <= 24 * 60)
            {
                return ttl;
            }
            return 60;
        }

        private string BuildResetLink(string token)
        {
            var baseUrl = _config["Frontend:BaseUrl"] ?? _config["App:FrontendBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:3000";
            }

            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}

// M.B