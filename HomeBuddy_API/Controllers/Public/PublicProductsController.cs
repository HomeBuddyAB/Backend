
using HomeBuddy_API.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/products")]
public class PublicProductsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public PublicProductsController(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    // SKU-first listing (returns paged body with totalCount for UI pagination)
    [HttpGet]
    [ProducesResponseType(typeof(DTOs.Responses.ProductListPageResponse), 200)]
    public async Task<IActionResult> List([FromQuery] PublicListQuery q, CancellationToken ct)
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
        var res = await client.GetAsync($"/api/products{qs}", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new ContentResult
        {
            StatusCode = (int)res.StatusCode,
            ContentType = res.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = body
        };
    }

    /// <summary>
    /// Returns discounted products only (variants where ListPrice is set higher than Price).
    /// Useful for a "Deals" or "On Sale" page in the frontend.
    /// </summary>
    [HttpGet("deals")]
    [ProducesResponseType(typeof(DTOs.Responses.ProductListPageResponse), 200)]
    public async Task<IActionResult> Deals([FromQuery] PublicListQuery q, CancellationToken ct)
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
        var res = await client.GetAsync($"/api/products/deals{qs}", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new ContentResult
        {
            StatusCode = (int)res.StatusCode,
            ContentType = res.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = body
        };
    }
}
