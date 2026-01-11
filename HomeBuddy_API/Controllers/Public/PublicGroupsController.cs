using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/groups")]
public class PublicGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PublicGroupsController(ApplicationDbContext db) { _db = db; }

    // GET /groups/{idOrSlug}
    [HttpGet("{idOrSlug}")]
    public async Task<IActionResult> GetGroup(string idOrSlug, [FromQuery] string? sku, [FromQuery] string? color, [FromQuery] string? size,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort = "price", [FromQuery] string? dir = "asc",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        ProductGroup? group;

        if (Guid.TryParse(idOrSlug, out var guid))
        {
            group = await _db.ProductGroups
                .Include(g => g.Category)
                .FirstOrDefaultAsync(g => g.Id == guid, ct);
        }
        else
        {
            group = await _db.ProductGroups
                .Include(g => g.Category)
                .FirstOrDefaultAsync(g => g.Slug == idOrSlug || g.ObjectId == idOrSlug, ct);
        }

        if (group == null) return NotFound();

        var variants = _db.Variants
            .Include(v => v.Inventory)
            .Include(v => v.VariantImages)
            .Where(v => v.ProductGroupId == group.Id && !v.IsDeleted);

        var anyActive = await variants.AnyAsync(ct);
        if (!anyActive) return NotFound(); // public policy

        if (!string.IsNullOrWhiteSpace(color)) variants = variants.Where(v => v.Color == color);
        if (!string.IsNullOrWhiteSpace(size)) variants = variants.Where(v => v.Size == size);
        if (minPrice.HasValue) variants = variants.Where(v => v.Price >= minPrice.Value);
        if (maxPrice.HasValue) variants = variants.Where(v => v.Price <= maxPrice.Value);

        var dirVal = (dir ?? "asc").ToLower() == "desc" ? -1 : 1;
        variants = (sort ?? "price").ToLower() switch
        {
            "size" => dirVal == 1 ? variants.OrderBy(v => v.Size) : variants.OrderByDescending(v => v.Size),
            "color" => dirVal == 1 ? variants.OrderBy(v => v.Color) : variants.OrderByDescending(v => v.Color),
            _ => dirVal == 1 ? variants.OrderBy(v => v.Price) : variants.OrderByDescending(v => v.Price),
        };

        var total = await variants.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await variants.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        string ResolvePrimary(HomeBuddy_API.Models.Variant v)
        {
            var skuPrimary = v.VariantImages.Where(i => i.IsPrimary).OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();
            if (!string.IsNullOrEmpty(skuPrimary)) return skuPrimary;
            var colorPrimary = _db.ColorImages.Where(ci => ci.ProductGroupId == v.ProductGroupId && ci.Color == v.Color && ci.IsPrimary)
                                              .OrderBy(ci => ci.SortOrder).Select(ci => ci.Url).FirstOrDefault();
            return colorPrimary ?? string.Empty;
        }

        var globalMin = await _db.Variants.Where(v => v.ProductGroupId == group.Id && !v.IsDeleted).MinAsync(v => v.Price, ct);
        var globalMax = await _db.Variants.Where(v => v.ProductGroupId == group.Id && !v.IsDeleted).MaxAsync(v => v.Price, ct);

        var resp = new GroupPageResponse
        {
            ObjectId = group.ObjectId,
            Slug = group.Slug,
            Name = group.Name,
            MainCategory = group.Category.Name,
            HeroImageUrl = list.Select(ResolvePrimary).FirstOrDefault() ?? await _db.ColorImages
                .Where(ci => ci.ProductGroupId == group.Id && ci.IsPrimary)
                .OrderBy(ci => ci.SortOrder).Select(ci => ci.Url).FirstOrDefaultAsync(ct),
            MinPrice = globalMin,
            MaxPrice = globalMax,
            InStockAny = await _db.Inventories.AnyAsync(i => _db.Variants.Any(v => v.Id == i.VariantId && v.ProductGroupId == group.Id && !v.IsDeleted) && i.Quantity > 0, ct),
            Page = page,
            PageSize = pageSize,
            TotalVariants = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Variants = list.Select(v => new VariantItem
            {
                Sku = v.Sku,
                Color = v.Color,
                Size = v.Size,
                Price = v.Price,
                InStock = v.Inventory.Quantity > 0,
                PrimaryImageUrl = ResolvePrimary(v),
                Description = v.Description,
                Brand = v.Brand,
                Material = v.Material,
                // NEW: Add images array for gallery
                Images = v.VariantImages
        .OrderBy(vi => vi.IsPrimary ? 0 : 1)
        .ThenBy(vi => vi.SortOrder)
        .Select(vi => new ImageItem
        {
            Url = vi.Url,
            AltText = vi.AltText,
            IsPrimary = vi.IsPrimary,
            SortOrder = vi.SortOrder
        })
        .ToList()
            }).ToList()
        };

        // facets
        var groupActive = _db.Variants.Where(v => v.ProductGroupId == group.Id && !v.IsDeleted);
        resp.Colors = await groupActive.GroupBy(v => v.Color).Select(g => new FacetItem { Value = g.Key, Count = g.Count() }).ToListAsync(ct);
        resp.Sizes = await groupActive.GroupBy(v => v.Size).Select(g => new FacetItem { Value = g.Key, Count = g.Count() }).ToListAsync(ct);
        resp.PriceFacet = new PriceFacet { GlobalMax = globalMax, GlobalMin = globalMin };

        return Ok(resp);
    }
}