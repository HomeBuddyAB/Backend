using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.ReviewDTOs
{
    public class ReviewCreateDto
    {
        [Required]
        public string ProductGroupSlug { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}