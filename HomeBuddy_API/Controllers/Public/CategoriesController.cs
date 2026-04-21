using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IEnumerable<CategoryResponse>> Get(
        [FromQuery] int page = 1,
        [FromQuery] bool parentsOnly = false,
        [FromQuery] bool leafOnly = false,
        CancellationToken ct = default)
    {
        var query = _db.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.Subcategories)
            .Include(c => c.ProductGroups)
            .AsQueryable();

        if (parentsOnly)
            query = query.Where(c => c.ParentCategoryId == null);
        if (leafOnly)
            query = query.Where(c => c.ParentCategoryId != null);

        return await query
            .OrderBy(c => c.Name)
            .Paginate(page)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                ParentCategorySlug = c.ParentCategory != null ? c.ParentCategory.Slug : null,
                SubcategoryCount = c.Subcategories.Count,
                ProductGroupCount = c.ProductGroups.Count
            })
            .ToListAsync(ct);
    }

    [HttpGet("{parentSlug}/subcategories")]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetSubcategories(string parentSlug, CancellationToken ct = default)
    {
        var parent = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ParentCategoryId == null && c.Slug == parentSlug, ct);
        if (parent == null) return NotFound();

        var subcategories = await _db.Categories
            .Where(c => c.ParentCategoryId == parent.Id)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = parent.Name,
                ParentCategorySlug = parent.Slug,
                SubcategoryCount = c.Subcategories.Count,
                ProductGroupCount = c.ProductGroups.Count
            })
            .ToListAsync(ct);

        return Ok(subcategories);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken ct = default)
    {
        var count = await _db.Categories.CountAsync(ct);
        return Ok(new { count });
    }
}
