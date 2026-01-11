using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models 
{ 
    public class ProductGroup
    {
        // Surrogate PK for stable FKs
        public Guid Id { get; set; } = Guid.NewGuid();

        // Admin-defined business key (editable, unique)
        [Required, MaxLength(100)]
        public string ObjectId { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Slug { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public ICollection<ColorImage> ColorImages { get; set; } = new List<ColorImage>();
    }
}
