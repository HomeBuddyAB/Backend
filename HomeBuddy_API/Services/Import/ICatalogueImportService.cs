namespace HomeBuddy_API.Services.Import;

public interface ICatalogueImportService
{
    /// <summary>
    /// Runs the external catalogue import (TestApi feed). Returns Conflict if another import is in progress.
    /// </summary>
    Task<CatalogueImportOutcome> RunExternalImportAsync(string triggeredBy, CancellationToken ct = default);

    /// <summary>
    /// Runs the built-in test data import. Returns Conflict if another import is in progress.
    /// </summary>
    Task<CatalogueImportOutcome> RunTestImportAsync(string triggeredBy, CancellationToken ct = default);

    bool IsImportInProgress { get; }
}

public sealed class CatalogueImportOutcome
{
    public bool Success { get; init; }
    public bool Conflict { get; init; }
    public bool NotConfigured { get; init; }
    public ImportResult? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public int? HttpStatus { get; init; }

    public static CatalogueImportOutcome Ok(ImportResult result) => new() { Success = true, Result = result };
    public static CatalogueImportOutcome Busy() => new() { Conflict = true };
    public static CatalogueImportOutcome MissingConfig() => new() { NotConfigured = true };
    public static CatalogueImportOutcome Failed(string message, int? httpStatus = null) =>
        new() { ErrorMessage = message, HttpStatus = httpStatus };
}
