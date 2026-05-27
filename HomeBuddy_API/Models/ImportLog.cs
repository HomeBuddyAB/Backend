using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models;

public class ImportLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Source { get; set; } = null!;

    [Required, MaxLength(50)]
    public string TriggeredBy { get; set; } = null!;

    public int ItemsStaged { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsSkipped { get; set; }
    public int AutoCategorized { get; set; }
    public int Uncategorized { get; set; }
    public int Orphaned { get; set; }
    public int Reappeared { get; set; }
    public int WarningCount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Completed";

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public long DurationMs { get; set; }
}
