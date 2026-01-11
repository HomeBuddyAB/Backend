
using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IEnumerable<CategoryResponse>> Get(CancellationToken ct)
    {
        var list = await _db.Categories.OrderBy(c => c.Name).ToListAsync(ct);
        return list.Select(c => new CategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken ct)
    {
        var count = await _db.Categories.CountAsync(ct);
        return Ok(new { count });
    }
}
