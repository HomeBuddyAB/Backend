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
    [Route("api/admin/groups/{groupId:guid}/color-images")]
    [Authorize(Roles = "Admin")]
    public class ColorImagesAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ColorImagesAdminController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetImages(Guid groupId, [FromQuery] string? color, CancellationToken ct)
        {
            var query = _db.ColorImages.Where(i => i.ProductGroupId == groupId);

            if (!string.IsNullOrWhiteSpace(color))
                query = query.Where(i => i.Color == color);

            var images = await query.OrderBy(i => i.Color).ThenBy(i => i.SortOrder).ToListAsync(ct);
            return Ok(images);
        }

        [HttpPost]
        public async Task<IActionResult> AddImage(Guid groupId, [FromBody] AddColorImageRequest req, CancellationToken ct)
        {
            var group = await _db.ProductGroups.FindAsync(new object?[] { groupId }, ct);
            if (group == null) return NotFound("ProductGroup not found");

            var image = new ColorImage
            {
                ProductGroupId = groupId,
                Color = req.Color,
                Url = req.Url,
                AltText = req.AltText,
                IsPrimary = req.IsPrimary,
                SortOrder = req.SortOrder
            };

            _db.ColorImages.Add(image);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetImages), new { groupId, color = req.Color }, image);
        }

        [HttpPut("{imageId:guid}")]
        public async Task<IActionResult> UpdateImage(Guid groupId, Guid imageId, [FromBody] UpdateColorImageRequest req, CancellationToken ct)
        {
            var image = await _db.ColorImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductGroupId == groupId, ct);

            if (image == null) return NotFound();

            image.Url = req.Url ?? image.Url;
            image.AltText = req.AltText;
            image.IsPrimary = req.IsPrimary;
            image.SortOrder = req.SortOrder;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> DeleteImage(Guid groupId, Guid imageId, CancellationToken ct)
        {
            var image = await _db.ColorImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductGroupId == groupId, ct);

            if (image == null) return NotFound();

            _db.ColorImages.Remove(image);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }

    public record AddColorImageRequest(string Color, string Url, string? AltText, bool IsPrimary, int SortOrder);
    public record UpdateColorImageRequest(string? Url, string? AltText, bool IsPrimary, int SortOrder);
}
