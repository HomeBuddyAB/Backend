using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class ResetPasswordDto
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }
    }
}

