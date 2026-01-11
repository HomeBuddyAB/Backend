
using System;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class ColorImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; } = null!;

        [Required, MaxLength(60)]
        public string Color { get; set; } = null!;

        [Required, MaxLength(2048)]
        public string Url { get; set; } = null!;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}