using HomeBuddy_API.DTOs.Requests.Auth;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HomeBuddy_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAuthRepository authRepo, ApplicationDbContext db, IConfiguration config, ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
            _db = db;
            _config = config;
            _logger = logger;
        }

        // User Registration
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: email already registered for {Email}", dto.Email);
                throw new Exception("Email already registered");
            }

            CreatePasswordHash(dto.Password, out string hash, out string salt);

            var mergedCart = MergeCarts("{}", dto.GuestCart);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hash,
                // Keep column for backwards-compatibility; empty means bcrypt hash stored in PasswordHash.
                PasswordSalt = string.Empty,
                Cart = mergedCart
            };

            await _authRepo.AddUserAsync(user);
            await _authRepo.SaveChangesAsync();

            _logger.LogInformation("User registered successfully with id {UserId} and email {Email}", user.Id, user.Email);

            return new AuthResponseDto
            {
                Email = user.Email,
                Token = GenerateJwtToken(user.Email, user.Id, "User"),
                Cart = mergedCart
            };
        }

        // User Login
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (user == null || !await VerifyAndUpgradePasswordIfNeededAsync(user, dto.Password))
            {
                _logger.LogWarning("Login failed for {Email}: invalid credentials", dto.Email);
                throw new Exception("Invalid credentials");
            }

            var mergedCart = MergeCarts(user.Cart ?? "{}", dto.GuestCart);
            if (mergedCart != (user.Cart ?? "{}"))
            {
                user.Cart = mergedCart;
                await _authRepo.SaveChangesAsync();
            }

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);

            return new AuthResponseDto
            {
                Email = user.Email,
                Token = GenerateJwtToken(user.Email, user.Id, "User"),
                Cart = mergedCart
            };
        }

        // Admin Login
        public async Task<AuthResponseDto> LoginAdminAsync(AdminLoginDto dto)
        {
            var admin = await _authRepo.GetAdminByUserNameAsync(dto.UserName);
            if (admin == null || !await VerifyAndUpgradePasswordIfNeededAsync(admin, dto.Password))
            {
                _logger.LogWarning("Admin login failed for {UserName}: invalid credentials", dto.UserName);
                throw new Exception("Invalid admin credentials");
            }

            return new AuthResponseDto
            {
                Email = admin.UserName,
                Token = GenerateJwtToken(admin.UserName, admin.Id, "Admin")
            };
        }
        // Helper Methods
        private void CreatePasswordHash(string password, out string hash, out string salt)
        {
            // bcrypt includes salt in the hash string; keep salt column empty for new hashes.
            salt = string.Empty;
            hash = BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPasswordLegacyHmac(string password, string storedHash, string storedSalt)
        {
            using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(computedHash) == storedHash;
        }

        private async Task<bool> VerifyAndUpgradePasswordIfNeededAsync(User user, string password)
        {
            // Legacy: HMACSHA512 + stored salt
            if (!string.IsNullOrWhiteSpace(user.PasswordSalt))
            {
                if (!VerifyPasswordLegacyHmac(password, user.PasswordHash, user.PasswordSalt))
                    return false;

                // Upgrade to bcrypt on successful login
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.PasswordSalt = string.Empty;
                await _authRepo.SaveChangesAsync();
                return true;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        private async Task<bool> VerifyAndUpgradePasswordIfNeededAsync(Admin admin, string password)
        {
            if (!string.IsNullOrWhiteSpace(admin.PasswordSalt))
            {
                if (!VerifyPasswordLegacyHmac(password, admin.PasswordHash, admin.PasswordSalt))
                    return false;

                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                admin.PasswordSalt = string.Empty;
                await _authRepo.SaveChangesAsync();
                return true;
            }

            return BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);
        }

        private static string GenerateResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            // URL-safe Base64 (no padding)
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Sha256Base64(string raw)
        {
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<string?> CreatePasswordResetTokenAsync(ForgotPasswordDto dto, CancellationToken ct = default)
        {
            var email = (dto.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var user = await _authRepo.GetUserByEmailAsync(email);
            if (user == null)
                return null; // do not leak existence

            // Invalidate previous outstanding tokens for this user
            var now = DateTimeOffset.UtcNow;
            var outstanding = _db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedUtc == null && t.ExpiresUtc > now);
            foreach (var t in outstanding)
                t.UsedUtc = now;

            var rawToken = GenerateResetToken();
            var tokenHash = Sha256Base64(rawToken);

            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresUtc = now.AddMinutes(30),
                UsedUtc = null,
                CreatedUtc = now
            });

            await _db.SaveChangesAsync(ct);
            return rawToken;
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
        {
            var email = (dto.Email ?? string.Empty).Trim();
            var tokenRaw = (dto.Token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(tokenRaw))
                throw new InvalidOperationException("Email and token are required.");

            var user = await _authRepo.GetUserByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("Invalid token.");

            var now = DateTimeOffset.UtcNow;
            var tokenHash = Sha256Base64(tokenRaw);

            var token = await _db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.TokenHash == tokenHash && t.UsedUtc == null && t.ExpiresUtc > now)
                .OrderByDescending(t => t.CreatedUtc)
                .FirstOrDefaultAsync(ct);

            if (token == null)
                throw new InvalidOperationException("Invalid token.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordSalt = string.Empty;
            token.UsedUtc = now;

            await _db.SaveChangesAsync(ct);
        }

        /// <summary>Merges guest cart into user cart by SKU. Same SKU: add quantities. Expects {"items":[{"sku":"...","quantity":n}]} or {"items":[{"sku":"...","qty":n}]}.</summary>
        private static string MergeCarts(string userCartJson, string? guestCartJson)
        {
            var merged = ParseCartToDict(userCartJson);
            if (string.IsNullOrWhiteSpace(guestCartJson))
                return SerializeCart(merged);

            var guestItems = ParseCartToDict(guestCartJson);
            foreach (var (sku, qty) in guestItems)
            {
                var key = NormalizeSku(sku);
                if (string.IsNullOrEmpty(key)) continue;
                merged[key] = merged.GetValueOrDefault(key, 0) + Math.Max(0, qty);
            }

            return SerializeCart(merged);
        }

        private static Dictionary<string, int> ParseCartToDict(string json)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                    return dict;

                foreach (var item in items.EnumerateArray())
                {
                    var sku = item.TryGetProperty("sku", out var s) ? s.GetString() : null;
                    var qty = 0;
                    if (item.TryGetProperty("quantity", out var q)) qty = q.TryGetInt32(out var qi) ? qi : 0;
                    else if (item.TryGetProperty("qty", out var q2)) qty = q2.TryGetInt32(out var qi2) ? qi2 : 0;

                    var key = NormalizeSku(sku);
                    if (string.IsNullOrEmpty(key)) continue;
                    dict[key] = dict.GetValueOrDefault(key, 0) + Math.Max(0, qty);
                }
            }
            catch { /* invalid JSON: return empty or parsed so far */ }
            return dict;
        }

        private static string NormalizeSku(string? sku) =>
            string.IsNullOrWhiteSpace(sku) ? "" : sku.Trim().ToUpperInvariant();

        private static string SerializeCart(Dictionary<string, int> items)
        {
            var arr = items
                .Where(kv => kv.Value > 0)
                .Select(kv => new { sku = kv.Key, quantity = kv.Value })
                .ToList();
            return JsonSerializer.Serialize(new { items = arr });
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