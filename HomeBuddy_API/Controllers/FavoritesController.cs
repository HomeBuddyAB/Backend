using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Requests.FavoriteDTOs;
using HomeBuddy_API.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeBuddy_API.Controllers
{
    /// <summary>
    /// User-specific favorites (wishlist) endpoints.
    /// All routes require an authenticated user with role "User".
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public FavoritesController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Get all favorited variants for the current user.
        /// Returns the same shape as the public SKU list for reuse on the frontend.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ProductListPageResponse), 200)]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var userId = GetUserId();

            var favoriteVariantIds = await _db.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.VariantId)
                .ToListAsync(ct);

            if (favoriteVariantIds.Count == 0)
            {
                return Ok(new ProductListPageResponse
                {
                    Items = new List<SkuListItemResponse>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 0,
                    TotalPages = 0
                });
            }

            var variants = await _db.Variants
                .Include(v => v.ProductGroup).ThenInclude(pg => pg.Category).ThenInclude(c => c.ParentCategory)
                .Include(v => v.Inventory)
                .Include(v => v.VariantImages)
                .Where(v => favoriteVariantIds.Contains(v.Id) && !v.IsDeleted && !v.ProductGroup.IsDeleted
                    && v.ProductGroup.PublishStatus == Models.PublishStatus.Published
                    && !v.ProductGroup.IsFeedSuspended)
                .OrderByDescending(v => v.ProductGroup.UpdatedAt)
                .ToListAsync(ct);

            string ResolvePrimary(Models.Variant v)
            {
                var skuPrimary = v.VariantImages
                    .Where(i => i.IsPrimary)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(skuPrimary))
                    return skuPrimary;

                var colorPrimary = _db.ColorImages
                    .Where(ci => ci.ProductGroupId == v.ProductGroupId && ci.Color == v.Color && ci.IsPrimary)
                    .OrderBy(ci => ci.SortOrder)
                    .Select(ci => ci.Url)
                    .FirstOrDefault();

                return colorPrimary ?? string.Empty;
            }

            var responses = new List<SkuListItemResponse>();

            foreach (var v in variants)
            {
                var primary = ResolvePrimary(v);
                var parentCategory = v.ProductGroup.Category.ParentCategory;
                var mainCategory = parentCategory?.Name ?? v.ProductGroup.Category.Name;
                var mainCategorySlug = parentCategory?.Slug ?? v.ProductGroup.Category.Slug;
                var slugOrObject = string.IsNullOrWhiteSpace(v.ProductGroup.Slug)
                    ? v.ProductGroup.ObjectId
                    : v.ProductGroup.Slug!;
                var groupPath = $"/groups/{slugOrObject}?sku={Uri.EscapeDataString(v.Sku)}";
                var siblingsCount = await _db.Variants.CountAsync(
                    x => x.ProductGroupId == v.ProductGroupId && !x.IsDeleted && x.Sku != v.Sku,
                    ct);

                responses.Add(new SkuListItemResponse
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    ObjectId = v.ProductGroup.ObjectId,
                    Slug = v.ProductGroup.Slug,
                    GroupName = v.ProductGroup.Name,
                    MainCategory = mainCategory,
                    CategorySlug = mainCategorySlug,
                    SubcategoryName = v.ProductGroup.Category.Name,
                    SubcategorySlug = v.ProductGroup.Category.Slug,
                    Color = v.Color,
                    Size = v.Size,
                    Price = v.Price,
                    InStock = v.Inventory.Quantity > 0,
                    PrimaryImageUrl = primary,
                    GroupLink = groupPath,
                    MoreVariantsCount = Math.Max(0, siblingsCount)
                });
            }

            return Ok(new ProductListPageResponse
            {
                Items = responses,
                TotalCount = responses.Count,
                Page = 1,
                PageSize = responses.Count,
                TotalPages = 1
            });
        }

        /// <summary>
        /// Get all favorited variant IDs for the current user.
        /// Useful for client-side toggles.
        /// </summary>
        [HttpGet("ids")]
        [ProducesResponseType(typeof(FavoriteIdsResponse), 200)]
        public async Task<IActionResult> GetIds(CancellationToken ct)
        {
            var userId = GetUserId();
            var ids = await _db.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.VariantId)
                .ToListAsync(ct);

            return Ok(new FavoriteIdsResponse { VariantIds = ids });
        }

        /// <summary>
        /// Check if a given variant is in the current user's favorites.
        /// Accepts either a variantId or a SKU.
        /// </summary>
        [HttpGet("check")]
        [ProducesResponseType(typeof(CheckFavoriteResponse), 200)]
        public async Task<IActionResult> Check([FromQuery] Guid? variantId, [FromQuery] string? sku, CancellationToken ct)
        {
            var userId = GetUserId();
            Guid? resolvedId = variantId;

            if (!resolvedId.HasValue && !string.IsNullOrWhiteSpace(sku))
            {
                var normalizedSku = sku.Trim().ToUpperInvariant();
                resolvedId = await _db.Variants
                    .Where(v => v.Sku == normalizedSku)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync(ct);
                if (resolvedId == Guid.Empty)
                    resolvedId = null;
            }

            if (!resolvedId.HasValue)
                return BadRequest(new { error = "Provide either variantId or sku." });

            var isFavorite = await _db.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.VariantId == resolvedId.Value, ct);

            return Ok(new CheckFavoriteResponse { IsFavorite = isFavorite });
        }

        /// <summary>
        /// Add a variant to the current user's favorites.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<IActionResult> Add([FromBody] AddFavoriteRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            Guid variantId;
            if (request.VariantId.HasValue)
            {
                variantId = request.VariantId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(request.Sku))
            {
                var normalizedSku = request.Sku.Trim().ToUpperInvariant();
                var id = await _db.Variants
                    .Where(v => v.Sku == normalizedSku)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync(ct);
                if (id == Guid.Empty)
                    return BadRequest(new { error = "Variant not found for the given SKU." });
                variantId = id;
            }
            else
            {
                return BadRequest(new { error = "Provide either variantId or sku." });
            }

            var exists = await _db.Variants
                .AnyAsync(v => v.Id == variantId && !v.IsDeleted, ct);
            if (!exists)
                return BadRequest(new { error = "Variant not found." });

            var already = await _db.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.VariantId == variantId, ct);
            if (already)
                return Ok(new { message = "Already in favorites." });

            _db.UserFavorites.Add(new Models.UserFavorite
            {
                UserId = userId,
                VariantId = variantId
            });
            await _db.SaveChangesAsync(ct);

            return StatusCode(201, new { variantId, message = "Added to favorites." });
        }

        /// <summary>
        /// Remove a variant from the current user's favorites.
        /// Accepts either variantId or sku as a query parameter.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Remove([FromQuery] Guid? variantId, [FromQuery] string? sku, CancellationToken ct)
        {
            var userId = GetUserId();
            Guid? resolvedId = variantId;

            if (!resolvedId.HasValue && !string.IsNullOrWhiteSpace(sku))
            {
                var normalizedSku = sku.Trim().ToUpperInvariant();
                resolvedId = await _db.Variants
                    .Where(v => v.Sku == normalizedSku)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync(ct);
                if (resolvedId == Guid.Empty)
                    resolvedId = null;
            }

            if (!resolvedId.HasValue)
                return BadRequest(new { error = "Provide either variantId or sku." });

            var entity = await _db.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.VariantId == resolvedId.Value, ct);

            if (entity == null)
                return NotFound(new { error = "Favorite not found." });

            _db.UserFavorites.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}

