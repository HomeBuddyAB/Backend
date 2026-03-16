using HomeBuddy_API.DTOs.Requests.Auth;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAuthRepository authRepo, IConfiguration config, ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
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
                PasswordSalt = salt,
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
            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
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
            if (admin == null || !VerifyPassword(dto.Password, admin.PasswordHash, admin.PasswordSalt))
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