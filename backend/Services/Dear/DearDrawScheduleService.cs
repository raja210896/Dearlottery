using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Dear;

/// <summary>
/// The one 7Dear sync job, in two phases:
/// A) on startup, a full catch-up from the configured historical start date through today
///    (<see cref="IDearBackfillService"/>, insert-only-missing);
/// B) then, every day, checks each draw slot on a draw-time-anchored schedule (draw+10 min,
///    then backoff retries), instead of the fixed-interval polling in
///    <see cref="SyncBackgroundService"/>. Always active — 7Dear needs no token/config to be
///    reachable. Reuses <see cref="ISyncService.SyncDateWithProviderAsync"/> for the actual
///    fetch/parse/insert/duplicate-check pipeline; no new insert logic here.
/// </summary>
public class DearDrawScheduleService : BackgroundService
{
    private static readonly (string DrawTime, TimeSpan ScheduledTime)[] Draws =
    {
        ("1 PM", new TimeSpan(13, 0, 0)),
        ("6 PM", new TimeSpan(18, 0, 0)),
        ("8 PM", new TimeSpan(20, 0, 0)),
    };

    // Minutes after the draw time for each attempt: initial check at draw+10, then retries
    // backing off by +10, +15, +20, +30 minutes between successive attempts.
    private static readonly int[] AttemptOffsetsMinutes = { 10, 20, 35, 55, 85 };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DearDrawScheduleService> _logger;

    public DearDrawScheduleService(IServiceScopeFactory scopeFactory, ILogger<DearDrawScheduleService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunFullSyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            foreach (var (drawTime, scheduledTime) in Draws)
            {
                if (stoppingToken.IsCancellationRequested) return;
                await RunDrawScheduleAsync(today, drawTime, scheduledTime, stoppingToken);
            }

            var now = DateTime.Now;
            var resumeAt = now.Date.AddDays(1).AddMinutes(1); // recompute tomorrow's schedule just after midnight
            await SafeDelay(resumeAt - now, stoppingToken);
        }
    }

    /// <summary>Mode A: catch up from the configured historical start date through today, once, on startup.</summary>
    private async Task RunFullSyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DearOptions>>().Value;
        var start = options.HistoricalStartDate;
        var end = DateOnly.FromDateTime(DateTime.Now);

        if (start > end) return; // nothing configured to catch up on

        var backfill = scope.ServiceProvider.GetRequiredService<IDearBackfillService>();
        try
        {
            await backfill.RunAsync(start, end, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[7Dear Sync] Full sync failed");
        }
    }

    private async Task RunDrawScheduleAsync(DateOnly date, string drawTime, TimeSpan scheduledTime, CancellationToken ct)
    {
        if (await ResultExistsAsync(date, drawTime, ct))
        {
            _logger.LogInformation("[7Dear] Result already exists ({DrawTime} {Date})", drawTime, date);
            return;
        }

        var drawDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(scheduledTime));
        var now = DateTime.Now;
        var attempts = AttemptOffsetsMinutes.Select(o => drawDateTime.AddMinutes(o)).Where(t => t >= now).ToList();
        if (attempts.Count == 0)
        {
            // Started after this draw's whole retry window already elapsed today — try once, now.
            attempts.Add(now);
        }

        foreach (var attemptAt in attempts)
        {
            var delay = attemptAt - DateTime.Now;
            if (delay > TimeSpan.Zero) await SafeDelay(delay, ct);
            if (ct.IsCancellationRequested) return;

            if (await ResultExistsAsync(date, drawTime, ct))
            {
                _logger.LogInformation("[7Dear] Result already exists ({DrawTime} {Date})", drawTime, date);
                return;
            }

            _logger.LogInformation("[7Dear] Fetch started ({DrawTime} {Date})", drawTime, date);

            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var provider = scope.ServiceProvider.GetRequiredService<DearLotteryCollectorService>();
            SyncOutcome outcome;
            try
            {
                outcome = await syncService.SyncDateWithProviderAsync(provider, date, $"7Dear-Scheduled-{drawTime}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[7Dear] Scrape failed ({DrawTime} {Date})", drawTime, date);
                continue;
            }

            if (!outcome.Success)
            {
                _logger.LogWarning("[7Dear] Scrape failed ({DrawTime} {Date}): {Message}", drawTime, date, outcome.Message);
                continue;
            }

            if (await ResultExistsAsync(date, drawTime, ct))
            {
                _logger.LogInformation("[7Dear] Result found: {DrawTime} {Date}", drawTime, date);
                _logger.LogInformation("[7Dear] Result inserted ({DrawTime} {Date})", drawTime, date);
                return;
            }

            _logger.LogInformation("[7Dear] Result unavailable - retry ({DrawTime} {Date})", drawTime, date);
        }

        _logger.LogInformation("[7Dear] Retries exhausted ({DrawTime} {Date}); manual entry required", drawTime, date);
    }

    private async Task<bool> ResultExistsAsync(DateOnly date, string drawTime, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LotteryResults.AnyAsync(r => r.DrawDate == date && r.DrawTime == drawTime, ct);
    }

    private static async Task SafeDelay(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero) return;
        try { await Task.Delay(delay, ct); } catch (TaskCanceledException) { }
    }
}
