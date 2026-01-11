using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Requests;
using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/variants")]
[Authorize(Roles = "Admin")]
public class VariantsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public VariantsAdminController(ApplicationDbContext db) { _db = db; }

    [HttpGet("by-group/{groupId:guid}")]
    public async Task<IActionResult> ByGroup(Guid groupId, int page, CancellationToken ct)
    {
        var v = await _db.Variants
            .Where(x => x.ProductGroupId == groupId)
            .Select(x => new VariantDto
            {
                Id = x.Id,
                Sku = x.Sku,
                Color = x.Color,
                Size = x.Size,
                Price = x.Price,
                InventoryQuantity = x.Inventory.Quantity,
                LastRestockDate = x.Inventory.LastRestockDate,
                Description = x.Description,
                Brand = x.Brand,
                Material = x.Material
            })
            .Paginate(page)
            .ToListAsync(ct);

        return Ok(v);
    }

    [HttpGet("by-group/count")]
    public async Task<IActionResult> CountByGroup(Guid groupId, CancellationToken ct)
    {
        var count = await _db.Variants.CountAsync(x => x.ProductGroupId == groupId, ct);
        return Ok(new { Count = count });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVariantRequest req, CancellationToken ct)
    {
        var group = await _db.ProductGroups.FindAsync(new object?[] { req.ProductGroupId }, ct);
        if (group == null) return BadRequest("Group not found.");
        if (await _db.Variants.AnyAsync(v => v.Sku == req.Sku, ct)) return Conflict("SKU exists.");

        var v = new Variant
        {
            Sku = req.Sku,
            ProductGroupId = req.ProductGroupId,
            Color = req.Color,
            Size = req.Size,
            Price = req.Price,
            Description = req.Description,
            Brand = req.Brand,
            Material = req.Material
        };
        _db.Variants.Add(v);
        await _db.SaveChangesAsync(ct);

        _db.Inventories.Add(new Inventory { VariantId = v.Id, Quantity = 0, LowStockThreshold = 0 });
        await _db.SaveChangesAsync(ct);

        // FIXED: Return a DTO with the variant data instead of CreatedAtAction
        var dto = new VariantDto
        {
            Id = v.Id,
            Sku = v.Sku,
            Color = v.Color,
            Size = v.Size,
            Price = v.Price,
            InventoryQuantity = 0,
            LastRestockDate = null,
            Description = v.Description,
            Brand = v.Brand,
            Material = v.Material
        };

        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVariantRequest req, CancellationToken ct)
    {
        var v = await _db.Variants.FindAsync(new object?[] { id }, ct);
        if (v == null) return NotFound();
        v.Color = req.Color;
        v.Size = req.Size;
        v.Price = req.Price;
        v.Description = req.Description;
        v.Brand = req.Brand;
        v.Material = req.Material;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var exists = await _db.Variants.AnyAsync(x => x.Id == id, ct);
        if (!exists) return NotFound();

        await _db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM InventoryTransactions 
        WHERE InventoryId IN (SELECT Id FROM Inventories WHERE VariantId = {0})
        
        DELETE FROM VariantImages WHERE VariantId = {0}
        DELETE FROM Inventories WHERE VariantId = {0}
        DELETE FROM Variants WHERE Id = {0}
     ", id);

        return NoContent();
    }

    [HttpPost("{id:guid}/inventory/adjust")]
    public async Task<IActionResult> AdjustInventory(Guid id, [FromBody] AdjustInventoryRequest req, CancellationToken ct)
    {
        var v = await _db.Variants.Include(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v == null) return NotFound();

        if (v.Inventory == null)
        {
            return BadRequest("Inventory not found for this variant");
        }

        v.Inventory.Quantity += req.Delta;

        if (req.TransactionType == InventoryTransactionType.Restock && req.Delta > 0)
        {
            v.Inventory.LastRestockDate = DateTimeOffset.UtcNow;
        }

        _db.Entry(v.Inventory).Property(x => x.LastRestockDate).IsModified = true;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            InventoryId = v.Inventory.Id,
            TransactionType = req.TransactionType,
            QuantityChange = req.Delta,
            ResultingQuantity = v.Inventory.Quantity,
            ReferenceId = req.ReferenceId
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            lastRestockDate = v.Inventory.LastRestockDate,
            quantity = v.Inventory.Quantity
        });
    }

    private static bool TrueFalse(bool True = false) => True;
}












