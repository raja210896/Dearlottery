using LotteryAnalytics.Api.Services.Results;

namespace LotteryAnalytics.Api.Services.Sambad;

public interface ISyncService
{
    Task<SyncOutcome> SyncTodayAsync(string trigger, CancellationToken ct = default);
    /// <summary>Syncs an arbitrary date using the DI-configured default provider.</summary>
    Task<SyncOutcome> SyncDateAsync(DateOnly date, string trigger, CancellationToken ct = default);
    /// <summary>Syncs an arbitrary date using an explicit provider override (e.g. historical backfill from a specific source).</summary>
    Task<SyncOutcome> SyncDateWithProviderAsync(IResultProvider provider, DateOnly date, string trigger, CancellationToken ct = default);
}

public class SyncOutcome
{
    public bool Success { get; set; }
    public int Imported { get; set; }
    /// <summary>Results returned by the provider (available + parseable) before duplicate filtering.</summary>
    public int Fetched { get; set; }
    /// <summary>Fetched results skipped because that DrawDate+DrawTime already existed.</summary>
    public int SkippedExisting { get; set; }
    public string? Message { get; set; }
}
