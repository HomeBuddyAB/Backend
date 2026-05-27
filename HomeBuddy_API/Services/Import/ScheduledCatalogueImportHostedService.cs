using Microsoft.Extensions.Options;

namespace HomeBuddy_API.Services.Import;

/// <summary>
/// Periodically imports products from the configured catalogue API (Catalogue:BaseUrl).
/// Shares the same import lock as manual admin imports.
/// </summary>
public class ScheduledCatalogueImportHostedService : BackgroundService
{
    private readonly ICatalogueImportService _importService;
    private readonly IConfiguration _config;
    private readonly CatalogueImportScheduleOptions _options;
    private readonly ILogger<ScheduledCatalogueImportHostedService> _logger;

    public ScheduledCatalogueImportHostedService(
        ICatalogueImportService importService,
        IConfiguration config,
        IOptions<CatalogueImportScheduleOptions> options,
        ILogger<ScheduledCatalogueImportHostedService> logger)
    {
        _importService = importService;
        _config = config;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Scheduled catalogue import is disabled.");
            return;
        }

        var baseUrl = _config["Catalogue:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning(
                "Scheduled catalogue import is enabled but Catalogue:BaseUrl is not configured. Scheduler will not run.");
            return;
        }

        var intervalMinutes = Math.Max(5, _options.IntervalMinutes);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "Scheduled catalogue import started. Interval={IntervalMinutes} min, BaseUrl={BaseUrl}, RunOnStartup={RunOnStartup}",
            intervalMinutes, baseUrl, _options.RunOnStartup);

        if (_options.RunOnStartup)
        {
            var startupDelay = TimeSpan.FromSeconds(Math.Max(0, _options.StartupDelaySeconds));
            try
            {
                await Task.Delay(startupDelay, stoppingToken);
                await RunScheduledImportAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunScheduledImportAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Scheduled catalogue import stopped.");
        }
    }

    private async Task RunScheduledImportAsync(CancellationToken ct)
    {
        _logger.LogInformation("Scheduled catalogue import tick starting.");

        var outcome = await _importService.RunExternalImportAsync("scheduler", ct);

        if (outcome.Conflict)
        {
            _logger.LogWarning("Scheduled catalogue import skipped: manual or concurrent import in progress.");
            return;
        }

        if (outcome.NotConfigured)
        {
            _logger.LogWarning("Scheduled catalogue import skipped: Catalogue:BaseUrl not configured.");
            return;
        }

        if (!outcome.Success)
        {
            _logger.LogWarning("Scheduled catalogue import failed: {Error}", outcome.ErrorMessage);
            return;
        }

        var r = outcome.Result!;
        _logger.LogInformation(
            "Scheduled catalogue import finished: staged={Staged}, updated={Updated}, orphaned={Orphaned}, unpublished={Unpublished}",
            r.Staged, r.Updated, r.Orphaned, r.Unpublished);
    }
}
