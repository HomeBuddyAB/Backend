namespace HomeBuddy_API.DTOs.Requests.AdminDashDTOs
{
    public class VariantDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = null!;
        public string Color { get; set; } = null!;
        public string Size { get; set; } = null!;
        public decimal Price { get; set; }

        public int InventoryQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public DateTimeOffset? LastRestockDate { get; set; }

        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Material { get; set; }
    }
}