using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    /// <summary>
    /// A single favorited product variant for a user (wishlist item).
    /// </summary>
    public class UserFavorite
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public Guid VariantId { get; set; }
        public Variant Variant { get; set; } = null!;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

