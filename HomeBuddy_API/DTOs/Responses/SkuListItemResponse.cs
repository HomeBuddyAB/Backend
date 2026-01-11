namespace HomeBuddy_API.DTOs.Responses;
public class SkuListItemResponse
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = null!;
    public string ObjectId { get; set; } = null!;
    public string? Slug { get; set; }      // Add slug for building group URLs
    public string GroupName { get; set; } = null!;
    public string MainCategory { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Size { get; set; } = null!;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string GroupLink { get; set; } = null!;
    public int MoreVariantsCount { get; set; }
}
