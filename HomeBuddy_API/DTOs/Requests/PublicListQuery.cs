
namespace HomeBuddy_API.DTOs.Requests;
public class PublicListQuery
{
    public string? CategorySlug { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; } = "price";
    public string? Dir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}
