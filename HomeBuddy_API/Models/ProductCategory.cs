using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        // Navigation
        public Product? Product { get; set; }
        public Category? Category { get; set; }
    }
}

