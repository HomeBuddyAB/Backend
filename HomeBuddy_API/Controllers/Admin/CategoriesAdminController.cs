using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Requests;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoriesAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll(
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

        var categories = await query
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.Name)
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

        return Ok(categories);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken ct = default)
    {
        var count = await _db.Categories.CountAsync(ct);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var name = req.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(name) : GenerateSlug(req.Slug);

        if (await _db.Categories.AnyAsync(c => c.Slug == slug, ct))
            return Conflict("Slug already exists.");

        if (req.ParentCategoryId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.Id == req.ParentCategoryId.Value, ct);
            if (!parentExists) return BadRequest("Parent category not found.");
        }

        var category = new Category
        {
            Name = name,
            Slug = slug,
            ParentCategoryId = req.ParentCategoryId
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return Ok(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = null,
            ParentCategorySlug = null,
            SubcategoryCount = 0,
            ProductGroupCount = 0
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var category = await _db.Categories.FindAsync(new object?[] { id }, ct);
        if (category == null) return NotFound();

        var name = req.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(name) : GenerateSlug(req.Slug);

        if (await _db.Categories.AnyAsync(c => c.Id != id && c.Slug == slug, ct))
            return Conflict("Slug already exists.");

        if (req.ParentCategoryId == id)
            return BadRequest("A category cannot be parent of itself.");

        if (req.ParentCategoryId.HasValue)
        {
            var parent = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == req.ParentCategoryId.Value, ct);
            if (parent == null) return BadRequest("Parent category not found.");
            if (parent.ParentCategoryId == id) return BadRequest("Cannot create parent loop.");
        }

        category.Name = name;
        category.Slug = slug;
        category.ParentCategoryId = req.ParentCategoryId;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories
            .Include(c => c.Subcategories)
            .Include(c => c.ProductGroups)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null) return NotFound();
        if (category.Subcategories.Count > 0)
            return BadRequest("Cannot delete category with subcategories.");
        if (category.ProductGroups.Count > 0)
            return BadRequest("Cannot delete category assigned to product groups.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string GenerateSlug(string input)
    {
        return new string(input.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .ToArray())
            .Replace(' ', '-')
            .Trim('-');
    }
}
