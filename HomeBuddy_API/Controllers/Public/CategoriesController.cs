
using HomeBuddy_API.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public CategoriesController(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    [HttpGet]
    public async Task<IEnumerable<CategoryResponse>> Get(CancellationToken ct)
    {
        var catalogueBase = _config["Catalogue:BaseUrl"];
        if (string.IsNullOrWhiteSpace(catalogueBase))
            return Enumerable.Empty<CategoryResponse>();

        var client = _httpFactory.CreateClient("catalogue");
        client.BaseAddress = new Uri(catalogueBase);

        var apiKey = _config["Catalogue:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        // We purposely keep the same response shape as today (CategoryResponse[]).
        var res = await client.GetAsync("/api/categories", ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<List<CategoryResponse>>(cancellationToken: ct);
        return payload ?? new List<CategoryResponse>();
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken ct)
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

        var res = await client.GetAsync("/api/categories/count", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new ContentResult
        {
            StatusCode = (int)res.StatusCode,
            ContentType = res.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = body
        };
    }
}
