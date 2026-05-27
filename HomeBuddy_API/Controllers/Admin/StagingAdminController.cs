using System.Security.Claims;
using HomeBuddy_API.Data;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Models;
using HomeBuddy_API.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/staging")]
[Authorize(Roles = "Admin")]
public class StagingAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ICatalogueImportService _catalogueImport;
    private readonly CatalogueImportScheduleOptions _scheduleOptions;

    public StagingAdminController(
        ApplicationDbContext db,
        IConfiguration config,
        ICatalogueImportService catalogueImport,
        IOptions<CatalogueImportScheduleOptions> scheduleOptions)
    {
        _db = db;
        _config = config;
        _catalogueImport = catalogueImport;
        _scheduleOptions = scheduleOptions.Value;
    }

    /// <summary>
    /// List staged/ready product groups for admin review.
    /// Supports filtering by publish status and whether the group is uncategorized.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] bool uncategorizedOnly = false,
        [FromQuery] bool incompleteOnly = false,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var query = _db.ProductGroups
            .Include(g => g.Category).ThenInclude(c => c.ParentCategory)
            .Include(g => g.Variants).ThenInclude(v => v.Inventory)
            .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
            .Include(g => g.ColorImages)
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PublishStatus>(status, true, out var ps))
            query = query.Where(g => g.PublishStatus == ps);
        else
            query = query.Where(g => g.PublishStatus != PublishStatus.Published);

        if (uncategorizedOnly)
            query = query.Where(g => g.Category.Slug == "uncategorized");

        var totalCount = await query.CountAsync(ct);
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var groups = await query
            .OrderByDescending(g => g.CreatedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);

        var items = groups.Select(g =>
        {
            var hasPrice = g.Variants.Any(v => v.Price > 0);
            var hasImage = g.Variants.Any(v => v.VariantImages.Any()) || g.ColorImages.Any();
            var hasStock = g.Variants.Any(v => v.Inventory != null && v.Inventory.Quantity > 0);
            var isUncategorized = g.Category.Slug == "uncategorized";

            var blockers = new List<string>();
            if (!hasPrice) blockers.Add("missing_price");
            if (!hasImage) blockers.Add("missing_image");
            if (isUncategorized) blockers.Add("uncategorized");

            var warnings = new List<string>();
            if (!hasStock) warnings.Add("zero_stock");

            var primaryImage = g.Variants
                .SelectMany(v => v.VariantImages)
                .OrderBy(i => i.IsPrimary ? 0 : 1)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault()
                ?? g.ColorImages
                    .OrderBy(i => i.IsPrimary ? 0 : 1)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault();

            return new
            {
                id = g.Id,
                objectId = g.ObjectId,
                name = g.Name,
                slug = g.Slug,
                publishStatus = g.PublishStatus.ToString(),
                rawCategoryHint = g.RawCategoryHint,
                importSource = g.ImportSource,
                categoryId = g.CategoryId,
                categoryName = g.Category.Name,
                categorySlug = g.Category.Slug,
                parentCategoryName = g.Category.ParentCategory?.Name,
                parentCategorySlug = g.Category.ParentCategory?.Slug,
                variantCount = g.Variants.Count,
                primaryImageUrl = primaryImage,
                isOrphaned = g.IsOrphaned,
                orphanedAt = g.OrphanedAt,
                createdAt = g.CreatedAt,
                blockers,
                warnings
            };
        });

        if (incompleteOnly)
            items = items.Where(r => r.blockers.Count > 0);

        return Ok(new
        {
            items = items.ToList(),
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var query = _db.ProductGroups.Where(g => !g.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PublishStatus>(status, true, out var ps))
            query = query.Where(g => g.PublishStatus == ps);
        else
            query = query.Where(g => g.PublishStatus != PublishStatus.Published);

        var count = await query.CountAsync(ct);
        return Ok(new { count });
    }

    /// <summary>
    /// Summary of staged items by status and blocker type.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var groups = await _db.ProductGroups
            .Include(g => g.Category)
            .Include(g => g.Variants).ThenInclude(v => v.Inventory)
            .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
            .Include(g => g.ColorImages)
            .Where(g => !g.IsDeleted && g.PublishStatus != PublishStatus.Published)
            .ToListAsync(ct);

        var staged = groups.Count(g => g.PublishStatus == PublishStatus.Staged);
        var ready = groups.Count(g => g.PublishStatus == PublishStatus.Ready);
        var rejected = groups.Count(g => g.PublishStatus == PublishStatus.Rejected);
        var uncategorized = groups.Count(g => g.Category.Slug == "uncategorized");
        var missingPrice = groups.Count(g => !g.Variants.Any(v => v.Price > 0));
        var missingImage = groups.Count(g => !g.Variants.Any(v => v.VariantImages.Any()) && !g.ColorImages.Any());
        var zeroStock = groups.Count(g => !g.Variants.Any(v => v.Inventory != null && v.Inventory.Quantity > 0));
        var orphaned = groups.Count(g => g.IsOrphaned);

        var feedSuspended = await _db.ProductGroups
            .CountAsync(g => !g.IsDeleted
                && g.PublishStatus == PublishStatus.Published
                && g.IsFeedSuspended, ct);

        return Ok(new
        {
            staged,
            ready,
            rejected,
            uncategorized,
            missingPrice,
            missingImage,
            zeroStock,
            orphaned,
            feedSuspended,
            total = groups.Count
        });
    }

    /// <summary>
    /// Assign a category to a staged product group.
    /// </summary>
    [HttpPut("{id:guid}/category")]
    public async Task<IActionResult> AssignCategory(Guid id, [FromBody] AssignCategoryRequest req, CancellationToken ct)
    {
        var group = await _db.ProductGroups.FindAsync(new object[] { id }, ct);
        if (group == null) return NotFound();

        var category = await _db.Categories.FindAsync(new object[] { req.CategoryId }, ct);
        if (category == null) return BadRequest("Category not found.");

        group.CategoryId = req.CategoryId;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(group.RawCategoryHint) && req.SaveMapping)
        {
            var existingMapping = await _db.CategoryMappings
                .FirstOrDefaultAsync(m => m.SupplierTerm == group.RawCategoryHint
                    && m.SupplierSource == group.ImportSource, ct);

            if (existingMapping == null)
            {
                _db.CategoryMappings.Add(new CategoryMapping
                {
                    SupplierTerm = group.RawCategoryHint,
                    SupplierSource = group.ImportSource,
                    SubcategoryId = req.CategoryId
                });
            }
        }

        // Auto-promote to Ready if assigning category resolved the last blocker
        if (group.PublishStatus == PublishStatus.Staged && category.Slug != "uncategorized")
        {
            await _db.Entry(group).Collection(g => g.Variants).LoadAsync(ct);
            foreach (var v in group.Variants)
                await _db.Entry(v).Collection(v2 => v2.VariantImages).LoadAsync(ct);

            bool hasPrice = group.Variants.Any(v => v.Price > 0);
            bool hasImage = group.Variants.Any(v => v.VariantImages.Any());

            if (hasPrice && hasImage)
                group.PublishStatus = PublishStatus.Ready;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Publish one or more product groups (move to Published status).
    /// Only succeeds if all blockers are resolved.
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] BulkIdsRequest req, CancellationToken ct)
    {
        var groups = await _db.ProductGroups
            .Include(g => g.Category)
            .Include(g => g.Variants).ThenInclude(v => v.Inventory)
            .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
            .Include(g => g.ColorImages)
            .Where(g => req.Ids.Contains(g.Id) && !g.IsDeleted)
            .ToListAsync(ct);

        var published = new List<Guid>();
        var blocked = new List<object>();

        foreach (var g in groups)
        {
            var blockers = new List<string>();
            if (!g.Variants.Any(v => v.Price > 0)) blockers.Add("missing_price");
            if (!g.Variants.Any(v => v.VariantImages.Any()) && !g.ColorImages.Any()) blockers.Add("missing_image");
            if (g.Category.Slug == "uncategorized") blockers.Add("uncategorized");

            if (blockers.Count > 0)
            {
                blocked.Add(new { id = g.Id, name = g.Name, blockers });
                continue;
            }

            g.PublishStatus = PublishStatus.Published;
            g.UpdatedAt = DateTimeOffset.UtcNow;
            published.Add(g.Id);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { published, blocked });
    }

    /// <summary>
    /// Reject one or more product groups (mark as Rejected).
    /// </summary>
    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] BulkIdsRequest req, CancellationToken ct)
    {
        var groups = await _db.ProductGroups
            .Where(g => req.Ids.Contains(g.Id) && !g.IsDeleted)
            .ToListAsync(ct);

        foreach (var g in groups)
        {
            g.PublishStatus = PublishStatus.Rejected;
            g.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { rejected = groups.Select(g => g.Id) });
    }

    /// <summary>
    /// Un-reject: move Rejected groups back to Staged for re-review.
    /// </summary>
    [HttpPost("restage")]
    public async Task<IActionResult> Restage([FromBody] BulkIdsRequest req, CancellationToken ct)
    {
        var groups = await _db.ProductGroups
            .Where(g => req.Ids.Contains(g.Id) && g.PublishStatus == PublishStatus.Rejected)
            .ToListAsync(ct);

        foreach (var g in groups)
        {
            g.PublishStatus = PublishStatus.Staged;
            g.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { restaged = groups.Select(g => g.Id) });
    }

    /// <summary>
    /// Bulk assign a category to multiple staged groups at once.
    /// </summary>
    [HttpPost("bulk-assign-category")]
    public async Task<IActionResult> BulkAssignCategory([FromBody] BulkAssignCategoryRequest req, CancellationToken ct)
    {
        var category = await _db.Categories.FindAsync(new object[] { req.CategoryId }, ct);
        if (category == null) return BadRequest("Category not found.");

        var groups = await _db.ProductGroups
            .Where(g => req.Ids.Contains(g.Id) && !g.IsDeleted)
            .ToListAsync(ct);

        foreach (var g in groups)
        {
            if (req.SaveMappings && !string.IsNullOrWhiteSpace(g.RawCategoryHint))
            {
                var exists = await _db.CategoryMappings
                    .AnyAsync(m => m.SupplierTerm == g.RawCategoryHint && m.SupplierSource == g.ImportSource, ct);
                if (!exists)
                {
                    _db.CategoryMappings.Add(new CategoryMapping
                    {
                        SupplierTerm = g.RawCategoryHint,
                        SupplierSource = g.ImportSource,
                        SubcategoryId = req.CategoryId
                    });
                }
            }

            g.CategoryId = req.CategoryId;
            g.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { assigned = groups.Count });
    }

    /// <summary>
    /// Run the built-in test import adapter. Creates staged product groups
    /// from hardcoded sample data for testing the staging pipeline.
    /// </summary>
    [HttpPost("import/test")]
    public async Task<IActionResult> RunTestImport(CancellationToken ct)
    {
        var triggeredBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var outcome = await _catalogueImport.RunTestImportAsync(triggeredBy, ct);

        if (outcome.Conflict)
            return Conflict(new { error = "An import is already in progress. Please wait." });

        if (!outcome.Success)
            return StatusCode(500, new { error = outcome.ErrorMessage });

        return Ok(outcome.Result);
    }

    [HttpPost("import/external")]
    public async Task<IActionResult> RunExternalImport(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_config["Catalogue:BaseUrl"]))
            return BadRequest(new { error = "Catalogue:BaseUrl is not configured in appsettings." });

        var triggeredBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var outcome = await _catalogueImport.RunExternalImportAsync(triggeredBy, ct);

        if (outcome.Conflict)
            return Conflict(new { error = "An import is already in progress. Please wait." });

        if (outcome.NotConfigured)
            return BadRequest(new { error = "Catalogue:BaseUrl is not configured in appsettings." });

        if (!outcome.Success)
            return StatusCode(outcome.HttpStatus ?? 500, new { error = outcome.ErrorMessage });

        return Ok(outcome.Result);
    }

    /// <summary>
    /// Scheduled import configuration and last automated run (if any).
    /// </summary>
    [HttpGet("import/schedule")]
    public async Task<IActionResult> GetImportSchedule(CancellationToken ct)
    {
        var baseUrl = _config["Catalogue:BaseUrl"];
        var intervalMinutes = Math.Max(5, _scheduleOptions.IntervalMinutes);

        var lastScheduled = await _db.ImportLogs
            .Where(l => l.TriggeredBy == "scheduler")
            .OrderByDescending(l => l.StartedAt)
            .Select(l => new
            {
                l.Id,
                l.StartedAt,
                l.CompletedAt,
                l.Status,
                l.ItemsStaged,
                l.ItemsUpdated,
                l.Orphaned,
                l.ErrorMessage
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            enabled = _scheduleOptions.Enabled,
            intervalMinutes,
            runOnStartup = _scheduleOptions.RunOnStartup,
            catalogueBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            importInProgress = _catalogueImport.IsImportInProgress,
            lastScheduledRun = lastScheduled
        });
    }

    /// <summary>
    /// Published listings hidden from the store due to supplier feed unavailability.
    /// </summary>
    [HttpGet("feed-suspended")]
    public async Task<IActionResult> GetFeedSuspended([FromQuery] int page = 1, CancellationToken ct = default)
    {
        var query = _db.ProductGroups
            .Include(g => g.Category).ThenInclude(c => c.ParentCategory)
            .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
            .Where(g => !g.IsDeleted
                && g.PublishStatus == PublishStatus.Published
                && g.IsFeedSuspended);

        var totalCount = await query.CountAsync(ct);
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var groups = await query
            .OrderByDescending(g => g.FeedSuspendedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            items = groups.Select(g => new
            {
                id = g.Id,
                objectId = g.ObjectId,
                name = g.Name,
                slug = g.Slug,
                importSource = g.ImportSource,
                categoryName = g.Category.Name,
                feedSuspendedAt = g.FeedSuspendedAt,
                primaryImageUrl = g.Variants
                    .SelectMany(v => v.VariantImages)
                    .OrderBy(i => i.IsPrimary ? 0 : 1)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            }),
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }

    /// <summary>
    /// Import history — list past import runs.
    /// </summary>
    [HttpGet("import/history")]
    public async Task<IActionResult> GetImportHistory([FromQuery] int page = 1, CancellationToken ct = default)
    {
        var totalCount = await _db.ImportLogs.CountAsync(ct);
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await _db.ImportLogs
            .OrderByDescending(l => l.StartedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            items = logs.Select(l => new
            {
                l.Id,
                l.Source,
                l.TriggeredBy,
                l.ItemsStaged,
                l.ItemsUpdated,
                l.ItemsSkipped,
                l.AutoCategorized,
                l.Uncategorized,
                l.Orphaned,
                l.Reappeared,
                l.WarningCount,
                l.Status,
                l.ErrorMessage,
                l.StartedAt,
                l.CompletedAt,
                l.DurationMs
            }),
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }

    /// <summary>
    /// Update a specific variant's data (price, description, brand, material, images).
    /// Used from the editorial review to fix incomplete data before publishing.
    /// </summary>
    [HttpPut("variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid variantId, [FromBody] StagingVariantUpdateRequest req, CancellationToken ct)
    {
        var variant = await _db.Variants
            .Include(v => v.ProductGroup)
            .Include(v => v.Inventory)
            .Include(v => v.VariantImages)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);

        if (variant == null) return NotFound();

        if (variant.ProductGroup.PublishStatus == PublishStatus.Published)
            return BadRequest(new { error = "Cannot edit a published variant through the staging endpoint." });

        if (req.Price.HasValue) variant.Price = req.Price.Value;
        if (req.ListPrice.HasValue) variant.ListPrice = req.ListPrice.Value;
        if (req.Description != null) variant.Description = req.Description;
        if (req.Brand != null) variant.Brand = req.Brand;
        if (req.Material != null) variant.Material = req.Material;
        if (req.Color != null) variant.Color = req.Color;
        if (req.Size != null) variant.Size = req.Size;
        if (req.Stock.HasValue && variant.Inventory != null) variant.Inventory.Quantity = req.Stock.Value;

        if (req.ImageUrls != null)
        {
            _db.VariantImages.RemoveRange(variant.VariantImages);

            for (int i = 0; i < req.ImageUrls.Count; i++)
            {
                _db.VariantImages.Add(new VariantImage
                {
                    Url = req.ImageUrls[i],
                    AltText = variant.ProductGroup.Name,
                    IsPrimary = i == 0,
                    SortOrder = i,
                    VariantId = variant.Id
                });
            }
        }

        variant.ProductGroup.UpdatedAt = DateTimeOffset.UtcNow;

        // Re-check if this resolves blockers → auto-promote to Ready
        if (variant.ProductGroup.PublishStatus == PublishStatus.Staged)
        {
            await _db.Entry(variant.ProductGroup).Collection(g => g.Variants).LoadAsync(ct);
            foreach (var v in variant.ProductGroup.Variants)
            {
                await _db.Entry(v).Collection(v2 => v2.VariantImages).LoadAsync(ct);
                if (v.Inventory == null) await _db.Entry(v).Reference(v2 => v2.Inventory).LoadAsync(ct);
            }

            var cat = await _db.Categories.FindAsync(new object[] { variant.ProductGroup.CategoryId }, ct);
            bool hasPrice = variant.ProductGroup.Variants.Any(v => v.Price > 0);
            bool hasImage = variant.ProductGroup.Variants.Any(v => v.VariantImages.Any());
            bool hasCat = cat?.Slug != "uncategorized";

            if (hasPrice && hasImage && hasCat)
                variant.ProductGroup.PublishStatus = PublishStatus.Ready;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Get all variants for a staged product group (for editing).
    /// </summary>
    [HttpGet("{groupId:guid}/variants")]
    public async Task<IActionResult> GetGroupVariants(Guid groupId, CancellationToken ct)
    {
        var group = await _db.ProductGroups
            .Include(g => g.Variants).ThenInclude(v => v.Inventory)
            .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted, ct);

        if (group == null) return NotFound();

        var variants = group.Variants.Select(v => new
        {
            v.Id,
            v.Sku,
            v.Color,
            v.Size,
            v.Price,
            v.ListPrice,
            v.Description,
            v.Brand,
            v.Material,
            stock = v.Inventory?.Quantity ?? 0,
            images = v.VariantImages
                .OrderBy(i => i.IsPrimary ? 0 : 1)
                .ThenBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder })
                .ToList()
        }).ToList();

        return Ok(variants);
    }
}

public class StagingVariantUpdateRequest
{
    public decimal? Price { get; set; }
    public decimal? ListPrice { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string? Material { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public int? Stock { get; set; }
    public List<string>? ImageUrls { get; set; }
}

public class AssignCategoryRequest
{
    public Guid CategoryId { get; set; }
    public bool SaveMapping { get; set; } = true;
}

public class BulkIdsRequest
{
    public List<Guid> Ids { get; set; } = new();
}

public class BulkAssignCategoryRequest
{
    public List<Guid> Ids { get; set; } = new();
    public Guid CategoryId { get; set; }
    public bool SaveMappings { get; set; } = true;
}
