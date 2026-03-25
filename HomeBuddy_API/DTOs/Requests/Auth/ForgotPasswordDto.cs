using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

