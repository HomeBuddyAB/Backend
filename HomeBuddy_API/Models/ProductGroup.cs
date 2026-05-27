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

        public PublishStatus PublishStatus { get; set; } = PublishStatus.Published;

        [MaxLength(500)]
        public string? RawCategoryHint { get; set; }

        [MaxLength(200)]
        public string? ImportSource { get; set; }

        public bool IsOrphaned { get; set; } = false;
        public DateTimeOffset? OrphanedAt { get; set; }

        /// <summary>
        /// Published items hidden from the storefront while the supplier feed is down.
        /// Cleared automatically when the product reappears in a successful import.
        /// </summary>
        public bool IsFeedSuspended { get; set; } = false;
        public DateTimeOffset? FeedSuspendedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public ICollection<ColorImage> ColorImages { get; set; } = new List<ColorImage>();
    }
}
