using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required, MaxLength(500)]
        public required string Url { get; set; }

        [MaxLength(255)]
        public string? AltText { get; set; }

        // Navigation
        public Product? Product { get; set; }
    }
}

