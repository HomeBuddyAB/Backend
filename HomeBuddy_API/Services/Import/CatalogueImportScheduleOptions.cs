namespace HomeBuddy_API.Services.Import;

/// <summary>
/// Configuration for automatic catalogue imports from Catalogue:BaseUrl.
/// </summary>
public class CatalogueImportScheduleOptions
{
    public const string SectionName = "Catalogue:ScheduledImport";

    public bool Enabled { get; set; } = false;

    /// <summary>How often to poll the catalogue feed (minimum 5 minutes).</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Run one import shortly after the API starts (when enabled).</summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>Delay before the first run when RunOnStartup is true.</summary>
    public int StartupDelaySeconds { get; set; } = 30;
}
