using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.OrderDTOs
{
    public class OrderUpdateDto
    {
        [StringLength(20)]
        public string? Status { get; set; } // e.g. Pending, Paid, Shipped, Cancelled

        [Range(0, double.MaxValue, ErrorMessage = "Total must be a positive value.")]
        public decimal? Total { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public List<OrderItemUpdateDto>? Items { get; set; }
    }

    public class OrderItemUpdateDto
    {
        public int Id { get; set; } // required for existing items

        [Range(1, 10000)]
        public int Quantity { get; set; }

        [Range(0, 100000)]
        public decimal UnitPrice { get; set; }
    }
}
