using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Requests;
using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.DTOs.Requests.GroupDTOs;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/groups")]
    [Authorize(Roles = "Admin")]
    public class GroupsAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public GroupsAdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, CancellationToken ct = default)
        {
            // Use Paginate extension to limit results
            var groups = await _db.ProductGroups
                .Include(g => g.Category)
                .Paginate(page)
                .Select(g => new ProductGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Slug = g.Slug ?? string.Empty,
                    ObjectId = g.ObjectId,
                    IsDeleted = g.IsDeleted,
                    Category = new CategoryDto
                    {
                        Id = g.Category.Id,
                        Name = g.Category.Name,
                        Slug = g.Category.Slug
                    }
                })
                .ToListAsync(ct);

            return Ok(groups);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount(CancellationToken ct = default)
        {
            var count = await _db.ProductGroups.CountAsync(ct);
            return Ok(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest req, CancellationToken ct = default)
        {
            if (await _db.ProductGroups.AnyAsync(g => g.ObjectId == req.ObjectId, ct))
                return Conflict("ObjectId already exists.");

            var cat = await _db.Categories.FindAsync(new object?[] { req.CategoryId }, ct);
            if (cat == null) return BadRequest("Category not found.");

            var group = new ProductGroup
            {
                ObjectId = req.ObjectId,
                Name = req.Name,
                CategoryId = req.CategoryId,
                Slug = GenerateSlug(req.Name)
            };

            _db.ProductGroups.Add(group);
            await _db.SaveChangesAsync(ct);

            // Map to DTO
            var dto = new ProductGroupDtoAdmin
            {
                Id = group.Id,
                ObjectId = group.ObjectId,
                Name = group.Name,
                Slug = group.Slug,
                CategoryId = group.CategoryId,
                IsDeleted = group.IsDeleted,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt
            };

            return CreatedAtAction(nameof(GetAll), new { id = group.Id }, dto);
        }


        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupRequest req, CancellationToken ct = default)
        {
            var group = await _db.ProductGroups.FindAsync(new object?[] { id }, ct);
            if (group == null) return NotFound();

            group.Name = req.Name;
            group.CategoryId = req.CategoryId;
            group.Slug = string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(req.Name) : req.Slug;
            group.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var exists = await _db.ProductGroups.AnyAsync(g => g.Id == id, ct);
            if (!exists) return NotFound();

            await _db.Database.ExecuteSqlRawAsync(@"
        DECLARE @VariantIds TABLE (Id UNIQUEIDENTIFIER)
        INSERT INTO @VariantIds SELECT Id FROM Variants WHERE ProductGroupId = {0}
        
        DELETE FROM InventoryTransactions 
        WHERE InventoryId IN (SELECT Id FROM Inventories WHERE VariantId IN (SELECT Id FROM @VariantIds))
        
        DELETE FROM Inventories WHERE VariantId IN (SELECT Id FROM @VariantIds)
        DELETE FROM VariantImages WHERE VariantId IN (SELECT Id FROM @VariantIds)
        DELETE FROM Variants WHERE ProductGroupId = {0}
        DELETE FROM ColorImages WHERE ProductGroupId = {0}
        DELETE FROM ProductGroups WHERE Id = {0}
    ", id);

            return NoContent();
        }

        private static string GenerateSlug(string input)
        {
            var s = new string(input.ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || c == ' ')
                .ToArray())
                .Replace(' ', '-');

            return s;
        }
    }
}
