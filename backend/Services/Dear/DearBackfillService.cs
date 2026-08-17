using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Dear;

public class DearBackfillSummary
{
    public int DatesProcessed { get; set; }
    public int RecordsImported { get; set; }
    public int MissingOrUnavailable { get; set; }
    public int ExistingDrawsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public interface IDearBackfillService
{
    Task<DearBackfillSummary> RunAsync(DateOnly start, DateOnly end, CancellationToken ct = default);
}

/// <summary>One-time/on-demand historical backfill, date-by-date, reusing the existing sync pipeline.</summary>
public class DearBackfillService : IDearBackfillService
{
    private const int DrawSlotsPerDay = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DearOptions _options;
    private readonly ILogger<DearBackfillService> _logger;

    public DearBackfillService(IServiceScopeFactory scopeFactory, IOptions<DearOptions> options, ILogger<DearBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DearBackfillSummary> RunAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var summary = new DearBackfillSummary();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var dearProvider = scope.ServiceProvider.GetRequiredService<DearLotteryCollectorService>();

            try
            {
                var outcome = await syncService.SyncDateWithProviderAsync(dearProvider, date, "Backfill-7Dear", ct);
                summary.DatesProcessed++;

                if (outcome.Success)
                {
                    summary.RecordsImported += outcome.Imported;
                    summary.ExistingDrawsSkipped += outcome.SkippedExisting;
                    summary.MissingOrUnavailable += Math.Max(0, DrawSlotsPerDay - outcome.Fetched);
                }
                else
                {
                    summary.Errors.Add($"{date:yyyy-MM-dd}: {outcome.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "7Dear backfill failed for {Date}", date);
                summary.Errors.Add($"{date:yyyy-MM-dd}: {ex.Message}");
            }
        }

        return summary;
    }
}
