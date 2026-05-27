using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("api/groups")]
public class PublicGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PublicGroupsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /groups/{idOrSlug}
    [HttpGet("{idOrSlug}")]
    public async Task<IActionResult> GetGroup(string idOrSlug, [FromQuery] string? sku, [FromQuery] string? color, [FromQuery] string? size,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort = "price", [FromQuery] string? dir = "asc",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var group = await _db.ProductGroups
            .Include(g => g.Category)
            .ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(g =>
                !g.IsDeleted &&
                g.PublishStatus == Models.PublishStatus.Published &&
                !g.IsFeedSuspended &&
                (g.ObjectId == idOrSlug || (g.Slug != null && g.Slug == idOrSlug)),
                ct);

        if (group is null)
            return NotFound();

        var allGroupVariants = await _db.Variants
            .Where(v => !v.IsDeleted && v.ProductGroupId == group.Id)
            .Include(v => v.Inventory)
            .Include(v => v.VariantImages)
            .ToListAsync(ct);

        if (allGroupVariants.Count == 0)
            return NotFound();

        var filtered = allGroupVariants.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(color))
            filtered = filtered.Where(v => string.Equals(v.Color, color, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(size))
            filtered = filtered.Where(v => string.Equals(v.Size, size, StringComparison.OrdinalIgnoreCase));
        if (minPrice.HasValue) filtered = filtered.Where(v => v.Price >= minPrice.Value);
        if (maxPrice.HasValue) filtered = filtered.Where(v => v.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(sku))
            filtered = filtered.OrderByDescending(v => string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase));

        var descending = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
        filtered = (sort ?? "price").ToLowerInvariant() switch
        {
            "size" => descending ? filtered.OrderByDescending(v => v.Size) : filtered.OrderBy(v => v.Size),
            "color" => descending ? filtered.OrderByDescending(v => v.Color) : filtered.OrderBy(v => v.Color),
            _ => descending ? filtered.OrderByDescending(v => v.Price) : filtered.OrderBy(v => v.Price),
        };

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = filtered.Count();
        var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        string PrimaryUrl(Models.Variant v)
        {
            var skuPrimary = v.VariantImages
                .OrderBy(i => i.IsPrimary ? 0 : 1)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(skuPrimary))
                return skuPrimary;

            var colorPrimary = _db.ColorImages
                .Where(ci => ci.ProductGroupId == group.Id && ci.Color == v.Color)
                .OrderBy(i => i.IsPrimary ? 0 : 1)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault();

            return colorPrimary ?? string.Empty;
        }

        var heroImage = paged.Select(PrimaryUrl).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u))
                        ?? allGroupVariants.Select(PrimaryUrl).FirstOrDefault();

        var colors = allGroupVariants
            .GroupBy(v => v.Color)
            .Select(g => new FacetItem { Value = g.Key, Count = g.Count() })
            .OrderBy(x => x.Value)
            .ToList();

        var sizes = allGroupVariants
            .GroupBy(v => v.Size)
            .Select(g => new FacetItem { Value = g.Key, Count = g.Count() })
            .OrderBy(x => x.Value)
            .ToList();

        var parentCategory = group.Category.ParentCategory;
        var mainCategory = parentCategory?.Name ?? group.Category.Name;
        var mainCategorySlug = parentCategory?.Slug ?? group.Category.Slug;

        var response = new GroupPageResponse
        {
            ObjectId = group.ObjectId,
            Slug = group.Slug,
            Name = group.Name,
            MainCategory = mainCategory,
            MainCategorySlug = mainCategorySlug,
            Subcategory = group.Category.Name,
            SubcategorySlug = group.Category.Slug,
            HeroImageUrl = heroImage,
            MinPrice = allGroupVariants.Min(v => v.Price),
            MaxPrice = allGroupVariants.Max(v => v.Price),
            InStockAny = allGroupVariants.Any(v => v.Inventory.Quantity > 0),
            Page = page,
            PageSize = pageSize,
            TotalVariants = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Variants = paged.Select(v => new VariantItem
            {
                Sku = v.Sku,
                Color = v.Color,
                Size = v.Size,
                Price = v.Price,
                ListPrice = v.ListPrice,
                InStock = v.Inventory.Quantity > 0,
                PrimaryImageUrl = PrimaryUrl(v),
                Description = v.Description,
                Brand = v.Brand,
                Material = v.Material,
                Images = v.VariantImages
                    .OrderBy(i => i.IsPrimary ? 0 : 1)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new ImageItem
                    {
                        Url = i.Url,
                        AltText = i.AltText,
                        IsPrimary = i.IsPrimary,
                        SortOrder = i.SortOrder
                    }).ToList()
            }).ToList(),
            Colors = colors,
            Sizes = sizes,
            PriceFacet = new PriceFacet
            {
                GlobalMin = allGroupVariants.Min(v => v.Price),
                GlobalMax = allGroupVariants.Max(v => v.Price)
            }
        };

        return Ok(response);
    }
}