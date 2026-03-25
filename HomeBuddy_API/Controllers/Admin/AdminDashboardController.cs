using HomeBuddy_API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public AdminDashboardController(ApplicationDbContext db, IConfiguration config, IHttpClientFactory httpFactory)
    {
        _db = db;
        _config = config;
        _httpFactory = httpFactory;
    }

    /// <summary>
    /// Lightweight aggregate statistics for the admin dashboard.
    /// Kept intentionally simple and fast – avoid heavy joins here.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = now.Date;

        var totalOrders = await _db.Orders.LongCountAsync(ct);
        var totalRevenue = await _db.Orders
            .Where(o => o.Status != null && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.Total ?? 0m, ct);

        var ordersToday = await _db.Orders
            .LongCountAsync(o => o.CreatedUtc >= todayStart, ct);

        var customers = await _db.Users.LongCountAsync(ct);

        long productGroups = 0;
        long variants = 0;
        long lowStockVariants = 0;
        long outOfStockVariants = 0;

        // Catalogue is external now; avoid reading catalogue from HomeBuddy DB.
        // If Catalogue:BaseUrl is configured, fetch a lightweight estimate using /api/products.
        var catalogueBase = _config["Catalogue:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(catalogueBase))
        {
            try
            {
                var client = _httpFactory.CreateClient("catalogue");
                client.BaseAddress = new Uri(catalogueBase);

                var apiKey = _config["Catalogue:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Remove("X-Api-Key");
                    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                }

                // Request a minimal page and use totalCount for variant count.
                var res = await client.GetFromJsonAsync<CatalogueListResponse>("/api/products?Page=1&PageSize=1", ct);
                variants = res?.TotalCount ?? 0;
            }
            catch
            {
                // Keep zeros if catalogue isn't reachable; dashboard should still render.
            }
        }

        var reviewsCount = await _db.Reviews.LongCountAsync(ct);
        var averageRating = await _db.Reviews.AnyAsync(ct)
            ? await _db.Reviews.AverageAsync(r => (double)r.Rating, ct)
            : 0d;

        return Ok(new
        {
            orders = new
            {
                total = totalOrders,
                totalRevenue,
                today = ordersToday
            },
            customers = new
            {
                total = customers
            },
            catalog = new
            {
                productGroups,
                variants,
                lowStockVariants,
                outOfStockVariants
            },
            reviews = new
            {
                total = reviewsCount,
                averageRating
            }
        });
    }

    private sealed class CatalogueListResponse
    {
        public long TotalCount { get; set; }
    }
}

