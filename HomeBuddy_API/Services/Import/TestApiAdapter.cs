using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeBuddy_API.Services.Import;

/// <summary>
/// Fetches product data from the external TestApi feed endpoint
/// and normalizes it into NormalizedRow format for the import engine.
/// </summary>
public class TestApiAdapter
{
    public const string SourceName = "test-api";

    private readonly HttpClient _http;
    private readonly string _feedUrl;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestApiAdapter(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _feedUrl = "/api/feed/products";
    }

    public async Task<List<NormalizedRow>> FetchAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync(_feedUrl, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var feedItems = JsonSerializer.Deserialize<List<FeedItem>>(json, JsonOpts)
                        ?? new List<FeedItem>();

        return feedItems.Select(ToNormalized).ToList();
    }

    private static NormalizedRow ToNormalized(FeedItem item)
    {
        return new NormalizedRow
        {
            ExternalId = item.ExternalId,
            Name = item.Name,
            RawCategoryHint = item.CategoryHint,
            Variants = item.Variants.Select(v => new NormalizedVariant
            {
                Sku = v.Sku,
                Color = v.Color,
                Size = v.Size,
                Price = v.Price,
                ListPrice = v.ListPrice,
                Stock = v.Stock,
                Description = v.Description,
                Brand = v.Brand,
                Material = v.Material,
                ImageUrls = v.Images ?? new List<string>()
            }).ToList()
        };
    }

    private class FeedItem
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CategoryHint { get; set; }
        public List<FeedVariant> Variants { get; set; } = new();
    }

    private class FeedVariant
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
        public List<string>? Images { get; set; }
    }
}
