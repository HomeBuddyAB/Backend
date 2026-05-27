using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class CategoryMapping
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string SupplierTerm { get; set; } = null!;

        [MaxLength(100)]
        public string? SupplierSource { get; set; }

        public Guid SubcategoryId { get; set; }
        public Category Subcategory { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
