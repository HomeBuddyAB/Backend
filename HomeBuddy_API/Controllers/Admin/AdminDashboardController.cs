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

    public AdminDashboardController(ApplicationDbContext db)
    {
        _db = db;
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

        var productGroups = await _db.ProductGroups.LongCountAsync(ct);
        var variants = await _db.Variants.LongCountAsync(ct);

        var lowStockThreshold = 5;
        var lowStockVariants = await _db.Inventories
            .CountAsync(i => i.Quantity > 0 && i.Quantity <= lowStockThreshold, ct);
        var outOfStockVariants = await _db.Inventories
            .CountAsync(i => i.Quantity == 0, ct);

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
}

