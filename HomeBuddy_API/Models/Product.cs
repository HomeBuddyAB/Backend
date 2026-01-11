using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBuddy_API.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public required string Name { get; set; }

        [Required, MaxLength(50)]
        public required string Sku { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Specs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 100)]
        public int Popularity { get; set; } = 1;

        // Navigation
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}

