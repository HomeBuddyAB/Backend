namespace HomeBuddy_API.Services.Import;

public class ImportResult
{
    public int Staged { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int AutoCategorized { get; set; }
    public int Uncategorized { get; set; }
    public int Orphaned { get; set; }
    public int Unpublished { get; set; }
    public int Reappeared { get; set; }
    public int FeedSuspended { get; set; }
    public int FeedRestored { get; set; }
    public bool OrphanAborted { get; set; }
    public bool FeedPauseApplied { get; set; }
    public List<ImportWarning> Warnings { get; set; } = new();
}

public class ImportWarning
{
    public string ExternalId { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}
