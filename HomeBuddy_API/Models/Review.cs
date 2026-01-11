using System.ComponentModel.DataAnnotations;
namespace HomeBuddy_API.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public Guid ProductGroupId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string Comment { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedUtc { get; set; } = null;

        // Optional navigation property
        public ProductGroup? ProductGroup { get; set; }
    }
}