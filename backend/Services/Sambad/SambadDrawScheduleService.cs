using LotteryAnalytics.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Sambad;

/// <summary>
/// Fetches each day's Sambad result on a draw-time-anchored schedule (draw+10 min, then
/// backoff retries) instead of the fixed-interval polling in <see cref="SyncBackgroundService"/>.
/// Idle (does nothing) unless Sambad:BaseUrl/Token are configured. Reuses the existing
/// <see cref="ISyncService"/> pipeline for fetching/parsing/inserting — no new insert or
/// duplicate-detection logic is introduced here.
/// </summary>
public class SambadDrawScheduleService : BackgroundService
{
    private static readonly (string DrawTime, TimeSpan ScheduledTime)[] Draws =
    {
        ("1 PM", new TimeSpan(13, 0, 0)),
        ("6 PM", new TimeSpan(18, 0, 0)),
        ("8 PM", new TimeSpan(20, 0, 0)),
    };

    // Minutes after the draw time for each attempt: initial fetch at draw+10, then retries
    // backing off by +10, +15, +20, +30 minutes between successive attempts.
    private static readonly int[] AttemptOffsetsMinutes = { 10, 20, 35, 55, 85 };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SambadOptions _options;
    private readonly ILogger<SambadDrawScheduleService> _logger;

    public SambadDrawScheduleService(IServiceScopeFactory scopeFactory, IOptions<SambadOptions> options, ILogger<SambadDrawScheduleService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configured = !string.IsNullOrWhiteSpace(_options.BaseUrl) && !string.IsNullOrWhiteSpace(_options.Token);
        if (!configured)
        {
            _logger.LogInformation("[Sambad] Draw-time scheduler idle: Sambad API not configured.");
            return;
        }

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

    private async Task RunDrawScheduleAsync(DateOnly date, string drawTime, TimeSpan scheduledTime, CancellationToken ct)
    {
        if (await ResultExistsAsync(date, drawTime, ct))
        {
            _logger.LogInformation("[Sambad] Result already exists ({DrawTime} {Date})", drawTime, date);
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
                _logger.LogInformation("[Sambad] Result already exists ({DrawTime} {Date})", drawTime, date);
                return;
            }

            _logger.LogInformation("[Sambad] Fetch started ({DrawTime} {Date})", drawTime, date);

            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            SyncOutcome outcome;
            try
            {
                outcome = await syncService.SyncDateAsync(date, $"Sambad-Scheduled-{drawTime}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sambad] Fetch failed ({DrawTime} {Date})", drawTime, date);
                continue;
            }

            if (!outcome.Success)
            {
                _logger.LogWarning("[Sambad] Fetch failed ({DrawTime} {Date}): {Message}", drawTime, date, outcome.Message);
                continue;
            }

            if (await ResultExistsAsync(date, drawTime, ct))
            {
                _logger.LogInformation("[Sambad] Result found ({DrawTime} {Date})", drawTime, date);
                _logger.LogInformation("[Sambad] Result inserted ({DrawTime} {Date})", drawTime, date);
                return;
            }

            _logger.LogInformation("[Sambad] Result unavailable - retry scheduled ({DrawTime} {Date})", drawTime, date);
        }

        _logger.LogInformation("[Sambad] Retries exhausted ({DrawTime} {Date}); manual entry required", drawTime, date);
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
