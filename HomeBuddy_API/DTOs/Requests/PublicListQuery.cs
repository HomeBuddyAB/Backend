
namespace HomeBuddy_API.DTOs.Requests;
public class PublicListQuery
{
    /// <summary>Optional search term; filters by product name, category, slug, SKU, or color.</summary>
    public string? Search { get; set; }
    /// <summary>Legacy category filter (kept for backwards compatibility).</summary>
    public string? CategorySlug { get; set; }
    /// <summary>Parent category slug for nested /shop/{category}/{subcategory} routes.</summary>
    public string? ParentCategorySlug { get; set; }
    /// <summary>Subcategory slug for nested /shop/{category}/{subcategory} routes.</summary>
    public string? SubcategorySlug { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; } = "price";
    public string? Dir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}
