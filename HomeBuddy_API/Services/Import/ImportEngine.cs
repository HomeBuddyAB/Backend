using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Services.Import;

public class ImportEngine
{
    private readonly ApplicationDbContext _db;

    public ImportEngine(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ImportResult> RunImport(List<NormalizedRow> rows, string sourceName, CancellationToken ct)
    {
        var result = new ImportResult();

        var mappings = await _db.CategoryMappings
            .Where(m => m.SupplierSource == null || m.SupplierSource == sourceName)
            .ToListAsync(ct);

        var categories = await _db.Categories.ToListAsync(ct);
        var uncategorized = categories.FirstOrDefault(c => c.Slug == "uncategorized");

        if (uncategorized == null)
        {
            uncategorized = new Category { Name = "Uncategorized", Slug = "uncategorized" };
            _db.Categories.Add(uncategorized);
            await _db.SaveChangesAsync(ct);
        }

        // Pre-load existing slugs and SKUs to avoid collisions
        var existingSlugs = new HashSet<string>(
            await _db.ProductGroups.Select(g => g.Slug!).Where(s => s != null).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var existingSkus = new HashSet<string>(
            await _db.Variants.Select(v => v.Sku).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ExternalId) || string.IsNullOrWhiteSpace(row.Name))
            {
                result.Skipped++;
                continue;
            }

            var existingGroup = await _db.ProductGroups
                .Include(g => g.Variants).ThenInclude(v => v.Inventory)
                .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
                .Include(g => g.ColorImages)
                .FirstOrDefaultAsync(g => g.ObjectId == row.ExternalId, ct);

            if (existingGroup != null && existingGroup.PublishStatus == PublishStatus.Published)
            {
                if (FeedSuspensionService.TryRestoreFeedSuspension(existingGroup))
                    result.FeedRestored++;

                result.Skipped++;
                continue;
            }

            if (existingGroup != null && existingGroup.PublishStatus == PublishStatus.Rejected)
            {
                result.Skipped++;
                continue;
            }

            var resolvedCategory = ResolveCategory(row.RawCategoryHint, sourceName, mappings, categories, uncategorized);
            bool isAutoCategorized = resolvedCategory.Id != uncategorized.Id;

            if (existingGroup != null)
            {
                var uniqueSlug = MakeUniqueSlug(row.Name, existingSlugs, existingGroup.Slug);
                existingSlugs.Add(uniqueSlug);

                existingGroup.Name = row.Name;
                existingGroup.Slug = uniqueSlug;
                existingGroup.CategoryId = resolvedCategory.Id;
                existingGroup.RawCategoryHint = row.RawCategoryHint;
                existingGroup.ImportSource = sourceName;
                existingGroup.UpdatedAt = DateTimeOffset.UtcNow;
                UpsertVariants(existingGroup, row, existingSkus);
                result.Updated++;
            }
            else
            {
                var uniqueSlug = MakeUniqueSlug(row.Name, existingSlugs);
                existingSlugs.Add(uniqueSlug);

                var group = new ProductGroup
                {
                    ObjectId = row.ExternalId,
                    Name = row.Name,
                    Slug = uniqueSlug,
                    CategoryId = resolvedCategory.Id,
                    PublishStatus = PublishStatus.Staged,
                    RawCategoryHint = row.RawCategoryHint,
                    ImportSource = sourceName,
                };

                _db.ProductGroups.Add(group);
                await _db.SaveChangesAsync(ct);

                await AddVariants(group, row, existingSkus, ct);
                result.Staged++;
            }

            if (isAutoCategorized) result.AutoCategorized++;
            else result.Uncategorized++;

            // Auto-promote to Ready if all blockers are clear
            var targetGroup = existingGroup ?? await _db.ProductGroups
                .Include(g => g.Category)
                .Include(g => g.Variants).ThenInclude(v => v.VariantImages)
                .FirstOrDefaultAsync(g => g.ObjectId == row.ExternalId, ct);

            if (targetGroup != null && targetGroup.PublishStatus == PublishStatus.Staged)
            {
                bool hasPrice = row.Variants.Any(v => v.Price > 0);
                bool hasImage = row.Variants.Any(v => v.ImageUrls.Count > 0);
                bool hasCat = targetGroup.Category?.Slug != "uncategorized";

                if (hasPrice && hasImage && hasCat)
                    targetGroup.PublishStatus = PublishStatus.Ready;
            }

            var warnings = new List<string>();
            if (!row.Variants.Any(v => v.Price > 0)) warnings.Add("missing_price");
            if (!row.Variants.Any(v => v.ImageUrls.Count > 0)) warnings.Add("missing_image");
            if (!row.Variants.Any(v => v.Stock > 0)) warnings.Add("zero_stock");

            if (warnings.Count > 0)
            {
                result.Warnings.Add(new ImportWarning
                {
                    ExternalId = row.ExternalId,
                    Issues = warnings
                });
            }
        }

        // Orphan / feed-pause: handle supplier sync after ingest.
        // Rejected items are excluded (already decided on).
        var feedExternalIds = new HashSet<string>(
            rows.Where(r => !string.IsNullOrWhiteSpace(r.ExternalId)).Select(r => r.ExternalId),
            StringComparer.OrdinalIgnoreCase);

        var sourceGroups = await _db.ProductGroups
            .Where(g => g.ImportSource == sourceName && !g.IsDeleted
                && g.PublishStatus != PublishStatus.Rejected)
            .ToListAsync(ct);

        var publishedFromSource = sourceGroups.Where(g => g.PublishStatus == PublishStatus.Published).ToList();
        var wouldOrphanPublished = publishedFromSource.Count(g => !feedExternalIds.Contains(g.ObjectId));

        // 100% / empty feed: entire catalogue vanished — feed-pause (hide store, keep Published).
        var missingFromFeedCount = sourceGroups.Count(g => !feedExternalIds.Contains(g.ObjectId));
        bool fullCatalogMissing = sourceGroups.Count > 0 && missingFromFeedCount == sourceGroups.Count;
        bool emptyFeed = feedExternalIds.Count == 0;
        bool shouldFeedPause = fullCatalogMissing || (emptyFeed && publishedFromSource.Count > 0);

        // 50% rule (published only): partial feed failure — abort orphan/staged moves, no feed-pause.
        bool abortOrphans = !shouldFeedPause
            && publishedFromSource.Count > 0
            && (double)wouldOrphanPublished / publishedFromSource.Count > 0.5;

        if (shouldFeedPause)
        {
            result.FeedSuspended = FeedSuspensionService.SuspendPublished(publishedFromSource);
            result.FeedPauseApplied = true;
            result.Warnings.Add(new ImportWarning
            {
                ExternalId = "_SYSTEM",
                Issues = new List<string>
                {
                    fullCatalogMissing
                        ? $"Feed pause: entire supplier catalogue ({publishedFromSource.Count} published) hidden from store until feed recovers."
                        : $"Feed pause: empty feed response — {result.FeedSuspended} published listing(s) hidden from store until feed recovers."
                }
            });
        }
        else if (abortOrphans)
        {
            result.OrphanAborted = true;
            result.Warnings.Add(new ImportWarning
            {
                ExternalId = "_SYSTEM",
                Issues = new List<string>
                {
                    $"Orphan detection aborted: {wouldOrphanPublished} of {publishedFromSource.Count} published items would be orphaned (>50%). Possible feed issue."
                }
            });
        }
        else
        {
            // Healthy partial feed: real orphans → Staged + IsOrphaned (editorial), not feed-pause.
            foreach (var sg in sourceGroups)
            {
                if (feedExternalIds.Contains(sg.ObjectId))
                {
                    if (sg.IsOrphaned)
                    {
                        sg.IsOrphaned = false;
                        sg.OrphanedAt = null;
                        result.Reappeared++;
                    }
                }
                else
                {
                    if (!sg.IsOrphaned)
                    {
                        sg.IsOrphaned = true;
                        sg.OrphanedAt = DateTimeOffset.UtcNow;
                        result.Orphaned++;
                    }

                    if (sg.PublishStatus == PublishStatus.Published)
                    {
                        sg.IsFeedSuspended = false;
                        sg.FeedSuspendedAt = null;
                        sg.PublishStatus = PublishStatus.Staged;
                        result.Unpublished++;
                    }
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    private Category ResolveCategory(
        string? rawHint,
        string sourceName,
        List<CategoryMapping> mappings,
        List<Category> categories,
        Category uncategorized)
    {
        if (string.IsNullOrWhiteSpace(rawHint))
            return uncategorized;

        var hint = rawHint.Trim();

        var mapping = mappings.FirstOrDefault(m =>
            string.Equals(m.SupplierTerm, hint, StringComparison.OrdinalIgnoreCase)
            && m.SupplierSource == sourceName);

        mapping ??= mappings.FirstOrDefault(m =>
            string.Equals(m.SupplierTerm, hint, StringComparison.OrdinalIgnoreCase)
            && m.SupplierSource == null);

        if (mapping != null)
        {
            var mapped = categories.FirstOrDefault(c => c.Id == mapping.SubcategoryId);
            if (mapped != null) return mapped;
        }

        var slugMatch = categories.FirstOrDefault(c =>
            string.Equals(c.Slug, GenerateSlug(hint), StringComparison.OrdinalIgnoreCase));
        if (slugMatch != null) return slugMatch;

        var nameMatch = categories.FirstOrDefault(c =>
            string.Equals(c.Name, hint, StringComparison.OrdinalIgnoreCase));
        if (nameMatch != null) return nameMatch;

        return uncategorized;
    }

    private async Task AddVariants(ProductGroup group, NormalizedRow row, HashSet<string> existingSkus, CancellationToken ct)
    {
        var groupVariants = await _db.Variants
            .Where(v => v.ProductGroupId == group.Id)
            .ToListAsync(ct);

        for (int i = 0; i < row.Variants.Count; i++)
        {
            var nv = row.Variants[i];
            var sku = ResolveOrGenerateSku(nv.Sku, row.ExternalId, i, existingSkus);

            var existing = groupVariants.FirstOrDefault(v =>
                string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                continue;

            var variant = new Variant
            {
                Sku = sku,
                Color = nv.Color ?? "Default",
                Size = nv.Size ?? "One Size",
                Price = nv.Price,
                ListPrice = nv.ListPrice,
                Description = nv.Description,
                Brand = nv.Brand,
                Material = nv.Material,
                ProductGroupId = group.Id,
                Inventory = new Inventory { Quantity = nv.Stock, LowStockThreshold = 5 }
            };

            _db.Variants.Add(variant);

            foreach (var url in nv.ImageUrls)
            {
                _db.Set<VariantImage>().Add(new VariantImage
                {
                    Url = url,
                    AltText = group.Name,
                    IsPrimary = nv.ImageUrls.IndexOf(url) == 0,
                    SortOrder = nv.ImageUrls.IndexOf(url),
                    VariantId = variant.Id
                });
            }
        }
    }

    private void UpsertVariants(ProductGroup group, NormalizedRow row, HashSet<string> existingSkus)
    {
        for (int i = 0; i < row.Variants.Count; i++)
        {
            var nv = row.Variants[i];
            var sku = ResolveOrGenerateSku(nv.Sku, row.ExternalId, i, existingSkus);

            var existing = group.Variants.FirstOrDefault(v =>
                string.Equals(v.Sku, sku, StringComparison.OrdinalIgnoreCase));

            // Also check if the original supplier SKU matches an existing variant
            existing ??= !string.IsNullOrWhiteSpace(nv.Sku)
                ? group.Variants.FirstOrDefault(v =>
                    string.Equals(v.Sku, nv.Sku, StringComparison.OrdinalIgnoreCase))
                : null;

            if (existing != null)
            {
                existing.Color = nv.Color ?? existing.Color;
                existing.Size = nv.Size ?? existing.Size;
                existing.Price = nv.Price;
                existing.ListPrice = nv.ListPrice;
                existing.Description = nv.Description ?? existing.Description;
                existing.Brand = nv.Brand ?? existing.Brand;
                existing.Material = nv.Material ?? existing.Material;

                if (existing.Inventory != null)
                    existing.Inventory.Quantity = nv.Stock;
            }
            else
            {
                var variant = new Variant
                {
                    Sku = sku,
                    Color = nv.Color ?? "Default",
                    Size = nv.Size ?? "One Size",
                    Price = nv.Price,
                    ListPrice = nv.ListPrice,
                    Description = nv.Description,
                    Brand = nv.Brand,
                    Material = nv.Material,
                    ProductGroupId = group.Id,
                    Inventory = new Inventory { Quantity = nv.Stock, LowStockThreshold = 5 }
                };
                _db.Variants.Add(variant);

                foreach (var url in nv.ImageUrls)
                {
                    _db.Set<VariantImage>().Add(new VariantImage
                    {
                        Url = url,
                        AltText = group.Name,
                        IsPrimary = nv.ImageUrls.IndexOf(url) == 0,
                        SortOrder = nv.ImageUrls.IndexOf(url),
                        VariantId = variant.Id
                    });
                }
            }
        }
    }

    /// <summary>
    /// If the supplier provided a SKU, check it against the global set.
    /// If it collides with an existing SKU (from a different group), append a suffix.
    /// If no SKU was provided, auto-generate one from the external ID + variant index.
    /// </summary>
    private static string ResolveOrGenerateSku(string? supplierSku, string externalId, int variantIndex, HashSet<string> existingSkus)
    {
        string baseSku;

        if (!string.IsNullOrWhiteSpace(supplierSku))
        {
            baseSku = supplierSku.Trim();

            if (!existingSkus.Contains(baseSku))
            {
                existingSkus.Add(baseSku);
                return baseSku;
            }

            // Collision — try suffixed versions
            for (int suffix = 2; suffix <= 999; suffix++)
            {
                var candidate = $"{baseSku}-{suffix}";
                if (!existingSkus.Contains(candidate))
                {
                    existingSkus.Add(candidate);
                    return candidate;
                }
            }

            // Extremely unlikely fallback
            var fallback = $"{baseSku}-{Guid.NewGuid().ToString("N")[..8]}";
            existingSkus.Add(fallback);
            return fallback;
        }

        // No SKU provided — generate from external ID
        baseSku = $"IMP-{externalId}-V{variantIndex + 1}";

        if (!existingSkus.Contains(baseSku))
        {
            existingSkus.Add(baseSku);
            return baseSku;
        }

        for (int suffix = 2; suffix <= 999; suffix++)
        {
            var candidate = $"{baseSku}-{suffix}";
            if (!existingSkus.Contains(candidate))
            {
                existingSkus.Add(candidate);
                return candidate;
            }
        }

        var autoFallback = $"{baseSku}-{Guid.NewGuid().ToString("N")[..8]}";
        existingSkus.Add(autoFallback);
        return autoFallback;
    }

    /// <summary>
    /// Generate a unique slug for a product group. If the base slug already exists,
    /// appends -2, -3, etc. Allows keeping the current slug if it belongs to this group.
    /// </summary>
    private static string MakeUniqueSlug(string name, HashSet<string> existingSlugs, string? currentSlug = null)
    {
        var baseSlug = GenerateSlug(name);

        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "product";

        // If the group already owns this slug, keep it
        if (currentSlug != null && string.Equals(baseSlug, currentSlug, StringComparison.OrdinalIgnoreCase))
            return currentSlug;

        if (!existingSlugs.Contains(baseSlug))
            return baseSlug;

        for (int suffix = 2; suffix <= 9999; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!existingSlugs.Contains(candidate))
                return candidate;
        }

        return $"{baseSlug}-{Guid.NewGuid().ToString("N")[..8]}";
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
