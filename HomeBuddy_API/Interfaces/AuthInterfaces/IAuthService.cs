using HomeBuddy_API.DTOs.Requests.Auth;
using System.Threading.Tasks;
using System.Threading;

namespace HomeBuddy_API.Interfaces.AuthInterfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> LoginAdminAsync(AdminLoginDto dto);
        /// <summary>
        /// Creates a one-time password reset token for the user if the email exists.
        /// Returns null to avoid leaking whether an email exists.
        /// </summary>
        Task<string?> CreatePasswordResetTokenAsync(ForgotPasswordDto dto, CancellationToken ct = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
    }
}

// M.B