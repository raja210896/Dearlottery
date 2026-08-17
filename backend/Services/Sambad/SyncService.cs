using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Notifications;
using LotteryAnalytics.Api.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Sambad;

public class SyncService : ISyncService
{
    private readonly IResultProvider _client;
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<SyncService> _logger;

    public SyncService(IResultProvider client, AppDbContext db, INotificationService notifications, ILogger<SyncService> logger)
    {
        _client = client;
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public Task<SyncOutcome> SyncTodayAsync(string trigger, CancellationToken ct = default) =>
        RunSyncAsync(_client, DateOnly.FromDateTime(DateTime.UtcNow), trigger, ct);

    public Task<SyncOutcome> SyncDateAsync(DateOnly date, string trigger, CancellationToken ct = default) =>
        RunSyncAsync(_client, date, trigger, ct);

    public Task<SyncOutcome> SyncDateWithProviderAsync(IResultProvider provider, DateOnly date, string trigger, CancellationToken ct = default) =>
        RunSyncAsync(provider, date, trigger, ct);

    private async Task<SyncOutcome> RunSyncAsync(IResultProvider provider, DateOnly date, string trigger, CancellationToken ct)
    {
        var log = new SyncLog { Trigger = trigger, StartedAt = DateTime.UtcNow };
        _db.SyncLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        var fetch = await provider.FetchResultsAsync(date, ct);

        if (!fetch.Success)
        {
            log.CompletedAt = DateTime.UtcNow;
            log.Success = false;
            log.Message = fetch.Error;
            await _db.SaveChangesAsync(ct);
            return new SyncOutcome { Success = false, Message = fetch.Error };
        }

        var existingKeys = await _db.LotteryResults
            .Where(r => r.DrawDate == date)
            .Select(r => r.DrawTime)
            .ToListAsync(ct);

        var imported = 0;
        var skippedExisting = 0;
        var newResults = new List<LotteryResult>();
        foreach (var item in fetch.Results)
        {
            if (existingKeys.Contains(item.DrawTime))
            {
                skippedExisting++;
                continue; // duplicate guard, keyed on DrawDate+DrawTime only (also enforced by unique index)
            }

            var entity = new LotteryResult
            {
                DrawDate = DateOnly.Parse(item.DrawDate),
                DrawTime = item.DrawTime,
                ResultValue = item.Result,
                Series = item.Series,
                Source = provider.SourceName,
                ImportedAt = DateTime.UtcNow
            };
            _db.LotteryResults.Add(entity);
            newResults.Add(entity);
            imported++;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Duplicate result(s) skipped during sync for {Date}", date);
        }

        foreach (var result in newResults)
        {
            try
            {
                await _notifications.SendResultNotificationAsync(result, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send result notification for {DrawTime}", result.DrawTime);
            }
        }

        log.CompletedAt = DateTime.UtcNow;
        log.Success = true;
        log.RecordsImported = imported;
        await _db.SaveChangesAsync(ct);

        return new SyncOutcome { Success = true, Imported = imported, Fetched = fetch.Results.Count, SkippedExisting = skippedExisting };
    }
}
