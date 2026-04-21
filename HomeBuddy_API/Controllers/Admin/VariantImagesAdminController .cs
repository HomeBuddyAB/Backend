using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/variants/{variantId:guid}/images")]
[Authorize(Roles = "Admin")]
public class VariantImagesAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public VariantImagesAdminController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetImages(Guid variantId, CancellationToken ct)
    {
        var images = await _db.VariantImages
            .Where(i => i.VariantId == variantId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);
        return Ok(images);
    }

    [HttpPost]
    public async Task<IActionResult> AddImage(Guid variantId, [FromBody] AddVariantImageRequest req, CancellationToken ct)
    {
        var variant = await _db.Variants.FindAsync(new object?[] { variantId }, ct);
        if (variant == null) return NotFound("Variant not found");

        var image = new VariantImage
        {
            VariantId = variantId,
            Url = req.Url,
            AltText = req.AltText,
            IsPrimary = req.IsPrimary,
            SortOrder = req.SortOrder
        };

        _db.VariantImages.Add(image);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetImages), new { variantId }, image);
    }

    [HttpPut("{imageId:guid}")]
    public async Task<IActionResult> UpdateImage(Guid variantId, Guid imageId, [FromBody] UpdateVariantImageRequest req, CancellationToken ct)
    {
        var image = await _db.VariantImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.VariantId == variantId, ct);
        if (image == null) return NotFound();

        image.Url = req.Url ?? image.Url;
        image.AltText = req.AltText;
        image.IsPrimary = req.IsPrimary;
        image.SortOrder = req.SortOrder;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid variantId, Guid imageId, CancellationToken ct)
    {
        var image = await _db.VariantImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.VariantId == variantId, ct);
        if (image == null) return NotFound();

        _db.VariantImages.Remove(image);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record AddVariantImageRequest(string Url, string? AltText, bool IsPrimary, int SortOrder);
public record UpdateVariantImageRequest(string? Url, string? AltText, bool IsPrimary, int SortOrder);