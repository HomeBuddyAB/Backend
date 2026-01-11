using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBuddy_API.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Order number cannot be longer than 30 characters.")]
        public string OrderNo { get; set; } = string.Empty;

        public int? UserId { get; set; } // optional user reference

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Total must be a positive value.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // e.g. Pending, Paid, Shipped, Cancelled

        [DataType(DataType.DateTime)]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}

