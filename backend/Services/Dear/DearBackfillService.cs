using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.EntityFrameworkCore;
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

/// <summary>
/// The one 7Dear sync job for a date range — used both for the initial start-date-to-today
/// catch-up and for on-demand admin backfills. Reuses the existing sync pipeline
/// (duplicate-safe, insert-only-missing, never overwrites a Manual record). Dates whose 3 draw
/// slots are already all in the database are skipped without any HTTP fetch.
/// </summary>
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
        _logger.LogInformation("[7Dear Sync] Started ({Start} to {End})", start, end);

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var existingCount = await db.LotteryResults.CountAsync(r => r.DrawDate == date, ct);
            if (existingCount >= DrawSlotsPerDay)
            {
                // All 3 draw slots for this date are already recorded — skip without fetching.
                _logger.LogInformation("[7Dear Sync] Already exists - skipped ({Date}, {Count} draws)", date, existingCount);
                summary.DatesProcessed++;
                summary.ExistingDrawsSkipped += existingCount;
                continue;
            }

            _logger.LogInformation("[7Dear Sync] Processing {Date}", date);

            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var dearProvider = scope.ServiceProvider.GetRequiredService<DearLotteryCollectorService>();

            try
            {
                var outcome = await syncService.SyncDateWithProviderAsync(dearProvider, date, "Backfill-7Dear", ct);
                summary.DatesProcessed++;

                if (outcome.Success)
                {
                    var unavailable = Math.Max(0, DrawSlotsPerDay - outcome.Fetched);
                    summary.RecordsImported += outcome.Imported;
                    summary.ExistingDrawsSkipped += outcome.SkippedExisting;
                    summary.MissingOrUnavailable += unavailable;

                    if (outcome.Imported > 0)
                    {
                        _logger.LogInformation("[7Dear Sync] Result found - inserting ({Date}, {Count} draws)", date, outcome.Imported);
                        _logger.LogInformation("[7Dear Sync] Inserted successfully ({Date})", date);
                    }
                    if (outcome.SkippedExisting > 0)
                    {
                        _logger.LogInformation("[7Dear Sync] Already exists - skipped ({Date}, {Count} draws)", date, outcome.SkippedExisting);
                    }
                    if (unavailable > 0)
                    {
                        _logger.LogInformation("[7Dear Sync] Result unavailable ({Date}, {Count} draws)", date, unavailable);
                    }
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

        _logger.LogInformation(
            "[7Dear Sync] Completed: Inserted={Inserted}, Skipped={Skipped}, Unavailable={Unavailable}",
            summary.RecordsImported, summary.ExistingDrawsSkipped, summary.MissingOrUnavailable);

        return summary;
    }
}
