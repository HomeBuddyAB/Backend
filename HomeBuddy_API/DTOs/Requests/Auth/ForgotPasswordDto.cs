using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}

