using System;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models 
{ 
    public class VariantImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid VariantId { get; set; }
        public Variant Variant { get; set; } = null!;

        [Required, MaxLength(2048)]
        public string Url { get; set; } = null!;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}