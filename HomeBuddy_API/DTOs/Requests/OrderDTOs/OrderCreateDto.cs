public class OrderCreateDto
{
    public string Email { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code of recipient (e.g. DE, FR). Required for VAT calculation.</summary>
    public string CountryCode { get; set; } = string.Empty;

    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
