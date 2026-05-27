using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Services.Import;

/// <summary>
/// Temporarily hides published listings from the storefront when the supplier feed
/// is unavailable. Items stay Published; only IsFeedSuspended gates visibility.
/// Auto-restored when the product reappears in a successful feed (ImportEngine).
/// </summary>
public static class FeedSuspensionService
{
    public static async Task<int> SuspendAllPublishedFromSourceAsync(
        ApplicationDbContext db,
        string sourceName,
        CancellationToken ct = default)
    {
        var published = await db.ProductGroups
            .Where(g => g.ImportSource == sourceName
                && !g.IsDeleted
                && g.PublishStatus == PublishStatus.Published
                && !g.IsFeedSuspended)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var g in published)
        {
            g.IsFeedSuspended = true;
            g.FeedSuspendedAt = now;
            g.UpdatedAt = now;
        }

        if (published.Count > 0)
            await db.SaveChangesAsync(ct);

        return published.Count;
    }

    public static int SuspendPublished(IEnumerable<ProductGroup> groups)
    {
        var now = DateTimeOffset.UtcNow;
        var count = 0;

        foreach (var g in groups)
        {
            if (g.PublishStatus != PublishStatus.Published || g.IsFeedSuspended)
                continue;

            g.IsFeedSuspended = true;
            g.FeedSuspendedAt = now;
            g.UpdatedAt = now;
            count++;
        }

        return count;
    }

    public static bool TryRestoreFeedSuspension(ProductGroup group)
    {
        if (!group.IsFeedSuspended)
            return false;

        group.IsFeedSuspended = false;
        group.FeedSuspendedAt = null;
        group.UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
