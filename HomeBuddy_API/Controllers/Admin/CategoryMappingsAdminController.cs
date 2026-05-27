using HomeBuddy_API.Data;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/category-mappings")]
[Authorize(Roles = "Admin")]
public class CategoryMappingsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoryMappingsAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, CancellationToken ct = default)
    {
        var mappings = await _db.CategoryMappings
            .Include(m => m.Subcategory).ThenInclude(c => c.ParentCategory)
            .OrderByDescending(m => m.CreatedAt)
            .Paginate(page, 20)
            .Select(m => new
            {
                m.Id,
                m.SupplierTerm,
                m.SupplierSource,
                subcategoryId = m.SubcategoryId,
                subcategoryName = m.Subcategory.Name,
                subcategorySlug = m.Subcategory.Slug,
                parentCategoryName = m.Subcategory.ParentCategory != null ? m.Subcategory.ParentCategory.Name : null,
                m.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(mappings);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken ct = default)
    {
        var count = await _db.CategoryMappings.CountAsync(ct);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMappingRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.SupplierTerm))
            return BadRequest("SupplierTerm is required.");

        var category = await _db.Categories.FindAsync(new object[] { req.SubcategoryId }, ct);
        if (category == null) return BadRequest("Subcategory not found.");

        var exists = await _db.CategoryMappings
            .AnyAsync(m => m.SupplierTerm == req.SupplierTerm.Trim() && m.SupplierSource == req.SupplierSource, ct);
        if (exists) return Conflict("Mapping already exists for this term/source combination.");

        var mapping = new CategoryMapping
        {
            SupplierTerm = req.SupplierTerm.Trim(),
            SupplierSource = req.SupplierSource?.Trim(),
            SubcategoryId = req.SubcategoryId
        };

        _db.CategoryMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);

        return Ok(new { mapping.Id, mapping.SupplierTerm, mapping.SupplierSource, mapping.SubcategoryId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMappingRequest req, CancellationToken ct)
    {
        var mapping = await _db.CategoryMappings.FindAsync(new object[] { id }, ct);
        if (mapping == null) return NotFound();

        var category = await _db.Categories.FindAsync(new object[] { req.SubcategoryId }, ct);
        if (category == null) return BadRequest("Subcategory not found.");

        mapping.SubcategoryId = req.SubcategoryId;
        if (!string.IsNullOrWhiteSpace(req.SupplierTerm))
            mapping.SupplierTerm = req.SupplierTerm.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mapping = await _db.CategoryMappings.FindAsync(new object[] { id }, ct);
        if (mapping == null) return NotFound();

        _db.CategoryMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public class CreateMappingRequest
{
    public string SupplierTerm { get; set; } = string.Empty;
    public string? SupplierSource { get; set; }
    public Guid SubcategoryId { get; set; }
}

public class UpdateMappingRequest
{
    public string? SupplierTerm { get; set; }
    public Guid SubcategoryId { get; set; }
}
