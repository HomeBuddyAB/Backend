using HomeBuddy_API.DTOs.Requests.Auth;
using HomeBuddy_API.DTOs.Responses.Auth;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace HomeBuddy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var response = await _authService.RegisterAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [EnableRateLimiting("auth")]
         [HttpPost("admin/login")]
        public async Task<IActionResult> LoginAdmin([FromBody] AdminLoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAdminAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns the authenticated user's identity (server-validated JWT).
        /// Frontend should prefer this over decoding JWT locally.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var role = User.FindFirstValue(ClaimTypes.Role);

            _ = int.TryParse(id, out var userId);

            return Ok(new MeResponseDto
            {
                Id = userId,
                Email = email,
                Role = role
            });
        }

        /// <summary>
        /// Request a password reset. Sends an email with a link when SMTP is configured.
        /// If SMTP is not configured, returns the raw token only in Development (for local testing).
        /// </summary>
        [EnableRateLimiting("auth")]
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
        {
            var token = await _authService.CreatePasswordResetTokenAsync(dto, ct);

            var smtpConfigured = !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]);
            if (token != null && !smtpConfigured && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return Ok(new { token });
            }

            return NoContent();
        }

        [EnableRateLimiting("auth")]
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto, ct);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

// M.B