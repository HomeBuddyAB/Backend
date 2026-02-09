namespace HomeBuddy_API.DTOs.Responses;

public class ProductListPageResponse
{
    public List<SkuListItemResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
