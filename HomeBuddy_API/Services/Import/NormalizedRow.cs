namespace HomeBuddy_API.Services.Import;

public class NormalizedRow
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RawCategoryHint { get; set; }
    public List<NormalizedVariant> Variants { get; set; } = new();
}

public class NormalizedVariant
{
    public string Sku { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public decimal? ListPrice { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Material { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
