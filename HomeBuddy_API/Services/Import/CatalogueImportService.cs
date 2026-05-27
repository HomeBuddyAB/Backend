using System.Diagnostics;
using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Services.Import;

public class CatalogueImportService : ICatalogueImportService
{
    private static readonly SemaphoreSlim ImportLock = new(1, 1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CatalogueImportService> _logger;

    public CatalogueImportService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<CatalogueImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public bool IsImportInProgress => ImportLock.CurrentCount == 0;

    public Task<CatalogueImportOutcome> RunTestImportAsync(string triggeredBy, CancellationToken ct = default) =>
        RunImportAsync(
            TestDataAdapter.SourceName,
            triggeredBy,
            async (engine, importCt) =>
            {
                var testData = TestDataAdapter.GetTestData();
                return await engine.RunImport(testData, TestDataAdapter.SourceName, importCt);
            },
            ct);

    public Task<CatalogueImportOutcome> RunExternalImportAsync(string triggeredBy, CancellationToken ct = default)
    {
        var baseUrl = _config["Catalogue:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return Task.FromResult(CatalogueImportOutcome.MissingConfig());

        return RunImportAsync(
            TestApiAdapter.SourceName,
            triggeredBy,
            async (engine, importCt) =>
            {
                var adapter = new TestApiAdapter(baseUrl);
                var rows = await adapter.FetchAsync(importCt);
                return await engine.RunImport(rows, TestApiAdapter.SourceName, importCt);
            },
            ct,
            onHttpError: ex => CatalogueImportOutcome.Failed(
                $"Could not reach catalogue at {baseUrl}: {ex.Message}",
                httpStatus: 502));
    }

    private async Task<CatalogueImportOutcome> RunImportAsync(
        string sourceName,
        string triggeredBy,
        Func<ImportEngine, CancellationToken, Task<ImportResult>> runEngine,
        CancellationToken ct,
        Func<HttpRequestException, CatalogueImportOutcome>? onHttpError = null)
    {
        if (!await ImportLock.WaitAsync(0, ct))
        {
            _logger.LogWarning("Catalogue import skipped ({Source}): another import is already in progress.", sourceName);
            return CatalogueImportOutcome.Busy();
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var log = new ImportLog
        {
            Source = sourceName,
            TriggeredBy = triggeredBy,
        };

        try
        {
            var sw = Stopwatch.StartNew();
            var engine = new ImportEngine(db);
            var result = await runEngine(engine, ct);
            sw.Stop();

            ApplyLogFromResult(log, result, sw.ElapsedMilliseconds);
            log.Status = "Completed";

            db.ImportLogs.Add(log);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Catalogue import completed ({Source}, triggered by {TriggeredBy}): staged={Staged}, updated={Updated}, feedSuspended={FeedSuspended}, feedRestored={FeedRestored}, orphaned={Orphaned}",
                sourceName, triggeredBy, result.Staged, result.Updated, result.FeedSuspended, result.FeedRestored, result.Orphaned);

            return CatalogueImportOutcome.Ok(result);
        }
        catch (HttpRequestException ex) when (onHttpError != null)
        {
            var suspended = await FeedSuspensionService.SuspendAllPublishedFromSourceAsync(db, sourceName, ct);
            if (suspended > 0)
            {
                _logger.LogWarning(
                    "Catalogue unreachable ({Source}): {Count} published listing(s) feed-suspended (hidden from store, data retained).",
                    sourceName, suspended);
            }

            await RecordFailureAsync(db, log, ex.Message, ct);
            _logger.LogError(ex, "Catalogue import failed ({Source}): HTTP error.", sourceName);
            return onHttpError(ex);
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(db, log, ex.Message, CancellationToken.None);
            _logger.LogError(ex, "Catalogue import failed ({Source}).", sourceName);
            throw;
        }
        finally
        {
            ImportLock.Release();
        }
    }

    private static void ApplyLogFromResult(ImportLog log, ImportResult result, long durationMs)
    {
        log.ItemsStaged = result.Staged;
        log.ItemsUpdated = result.Updated;
        log.ItemsSkipped = result.Skipped;
        log.AutoCategorized = result.AutoCategorized;
        log.Uncategorized = result.Uncategorized;
        log.Orphaned = result.Orphaned;
        log.Reappeared = result.Reappeared;
        log.WarningCount = result.Warnings.Count;
        log.DurationMs = durationMs;
        log.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static async Task RecordFailureAsync(ApplicationDbContext db, ImportLog log, string message, CancellationToken ct)
    {
        log.Status = "Failed";
        log.ErrorMessage = message.Length > 2000 ? message[..2000] : message;
        log.CompletedAt = DateTimeOffset.UtcNow;
        db.ImportLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
