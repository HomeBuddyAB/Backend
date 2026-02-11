using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    /// <summary>Address book entry: a customer saved by a user (B2B-style).</summary>
    public class SavedCustomer
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(300)]
        public string? StreetAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(2)]
        public string? CountryCode { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
