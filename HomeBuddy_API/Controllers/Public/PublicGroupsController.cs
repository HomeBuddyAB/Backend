using Microsoft.AspNetCore.Mvc;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/groups")]
public class PublicGroupsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public PublicGroupsController(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    // GET /groups/{idOrSlug}
    [HttpGet("{idOrSlug}")]
    public async Task<IActionResult> GetGroup(string idOrSlug, [FromQuery] string? sku, [FromQuery] string? color, [FromQuery] string? size,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort = "price", [FromQuery] string? dir = "asc",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var catalogueBase = _config["Catalogue:BaseUrl"];
        if (string.IsNullOrWhiteSpace(catalogueBase))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Catalogue:BaseUrl is not configured.");

        var client = _httpFactory.CreateClient("catalogue");
        client.BaseAddress = new Uri(catalogueBase);

        var apiKey = _config["Catalogue:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        var qs = HttpContext.Request.QueryString.HasValue ? HttpContext.Request.QueryString.Value : string.Empty;
        var res = await client.GetAsync($"/api/groups/{Uri.EscapeDataString(idOrSlug)}{qs}", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new ContentResult
        {
            StatusCode = (int)res.StatusCode,
            ContentType = res.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = body
        };
    }
}