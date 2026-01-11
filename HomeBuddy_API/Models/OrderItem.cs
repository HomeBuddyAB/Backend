using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HomeBuddy_API.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Quantity must be at least 1, and max 10,000.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Unit price must be positive, and under 100,000.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Match Variant.Id (Guid). Make nullable if items may not have a variant.
        public Guid? VariantId { get; set; }
        public Variant? Variant { get; set; }
    }
}

