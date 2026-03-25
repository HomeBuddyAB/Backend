using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Raw reset token returned by forgot-password (in Development) or delivered via email (later).</summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}

