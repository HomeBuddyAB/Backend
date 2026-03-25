using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// SHA-256 hash (Base64) of the raw reset token.
        /// Raw token is only shown once (optionally in Development).
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = null!;

        public DateTimeOffset ExpiresUtc { get; set; }

        public DateTimeOffset? UsedUtc { get; set; }

        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}

