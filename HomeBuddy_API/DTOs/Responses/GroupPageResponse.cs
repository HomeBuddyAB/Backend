using System.Collections.Generic;

namespace HomeBuddy_API.DTOs.Responses;
public class GroupPageResponse
{
    public string ObjectId { get; set; } = null!;
    public string? Slug { get; set; }      
    public string Name { get; set; } = null!;
    public string MainCategory { get; set; } = null!;
    public string? HeroImageUrl { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public bool InStockAny { get; set; }
    public List<VariantItem> Variants { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalVariants { get; set; }
    public int TotalPages { get; set; }
    public List<FacetItem> Colors { get; set; } = new();
    public List<FacetItem> Sizes { get; set; } = new();
    public PriceFacet PriceFacet { get; set; } = new();
}
public class ImageItem
{
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class VariantItem
{
    public string Sku { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Size { get; set; } = null!;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Material { get; set; }
    public List<ImageItem> Images { get; set; } = new();
}

public class FacetItem
{
    public string Value { get; set; } = null!;
    public int Count { get; set; }
}

public class PriceFacet
{
    public decimal GlobalMin { get; set; }
    public decimal GlobalMax { get; set; }
}
