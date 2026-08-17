using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Analysis;
using LotteryAnalytics.Api.Services.Notifications;
using LotteryAnalytics.Api.Services.Predictions;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Results;

/// <summary>
/// Validates and persists admin-entered results, then refreshes downstream analysis
/// (cache invalidation) and runs an automatic pre-draw candidate comparison.
/// </summary>
public class ManualResultService : IManualResultService
{
    private static readonly HashSet<string> ValidDrawTimes = new() { "1 PM", "6 PM", "8 PM" };

    private readonly AppDbContext _db;
    private readonly ICandidateScoringService _scoring;
    private readonly INotificationService _notifications;
    private readonly IPredictionService _predictions;
    private readonly ILogger<ManualResultService> _logger;

    public ManualResultService(AppDbContext db, ICandidateScoringService scoring, INotificationService notifications, IPredictionService predictions, ILogger<ManualResultService> logger)
    {
        _db = db;
        _scoring = scoring;
        _notifications = notifications;
        _predictions = predictions;
        _logger = logger;
    }

    public async Task<ManualResultOutcome> CreateAsync(DateOnly drawDate, string drawTime, string resultValue, CancellationToken ct = default)
    {
        var validation = Validate(drawDate, drawTime, resultValue);
        if (validation is not null) return ManualResultOutcome.Fail(validation);

        var duplicate = await _db.LotteryResults.AnyAsync(r => r.DrawDate == drawDate && r.DrawTime == drawTime, ct);
        if (duplicate) return ManualResultOutcome.Fail("A result for this draw date and time already exists.");

        // Compare against pre-draw statistical candidates before the new result is written,
        // so the comparison never sees itself (no future-data leakage).
        var matched = await TryMatchCandidateAsync(drawDate, drawTime, resultValue, ct);

        var entity = new LotteryResult
        {
            DrawDate = drawDate,
            DrawTime = drawTime,
            ResultValue = resultValue,
            Source = "Manual",
            ImportedAt = DateTime.UtcNow
        };
        _db.LotteryResults.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ManualResultOutcome.Fail("A result for this draw date and time already exists.");
        }

        await InvalidateAnalysisCacheAsync(drawTime, ct);

        try
        {
            await _predictions.EvaluatePendingAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate pending predictions for {DrawTime} {DrawDate}", drawTime, drawDate);
        }

        try
        {
            await _notifications.SendResultNotificationAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send result notification for manually entered {DrawTime} result", drawTime);
        }

        return new ManualResultOutcome { Success = true, Result = entity, MatchedCandidate = matched };
    }

    public async Task<ManualResultOutcome> UpdateAsync(int id, DateOnly drawDate, string drawTime, string resultValue, CancellationToken ct = default)
    {
        var validation = Validate(drawDate, drawTime, resultValue);
        if (validation is not null) return ManualResultOutcome.Fail(validation);

        var entity = await _db.LotteryResults.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return ManualResultOutcome.Fail("Result not found.");

        var duplicate = await _db.LotteryResults.AnyAsync(r => r.Id != id && r.DrawDate == drawDate && r.DrawTime == drawTime, ct);
        if (duplicate) return ManualResultOutcome.Fail("A result for this draw date and time already exists.");

        var oldDrawTime = entity.DrawTime;
        entity.DrawDate = drawDate;
        entity.DrawTime = drawTime;
        entity.ResultValue = resultValue;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ManualResultOutcome.Fail("A result for this draw date and time already exists.");
        }

        await InvalidateAnalysisCacheAsync(oldDrawTime, ct);
        if (drawTime != oldDrawTime) await InvalidateAnalysisCacheAsync(drawTime, ct);

        try
        {
            await _predictions.EvaluatePendingAsync(entity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate pending predictions for {DrawTime} {DrawDate}", drawTime, drawDate);
        }

        return new ManualResultOutcome { Success = true, Result = entity };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.LotteryResults.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return false;

        _db.LotteryResults.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await InvalidateAnalysisCacheAsync(entity.DrawTime, ct);
        return true;
    }

    private async Task<bool?> TryMatchCandidateAsync(DateOnly drawDate, string drawTime, string resultValue, CancellationToken ct)
    {
        var hasPriorHistory = await _db.LotteryResults.AnyAsync(r => r.DrawTime == drawTime && r.DrawDate < drawDate, ct);
        if (!hasPriorHistory) return null;

        var candidates = await _scoring.GetCandidatesAsync(drawTime, 2, null, drawDate.AddDays(-1), 10, ct);
        if (candidates.Candidates.Count == 0) return null;

        var actualLast2 = resultValue.Length <= 2 ? resultValue.PadLeft(2, '0') : resultValue[^2..];
        return candidates.Candidates.Any(c => c.Value == actualLast2);
    }

    private async Task InvalidateAnalysisCacheAsync(string drawTime, CancellationToken ct)
    {
        var stale = await _db.AnalysisSnapshots
            .Where(s => s.DrawTime == drawTime || s.DrawTime == "all")
            .ToListAsync(ct);
        if (stale.Count == 0) return;

        _db.AnalysisSnapshots.RemoveRange(stale);
        await _db.SaveChangesAsync(ct);
    }

    private static string? Validate(DateOnly drawDate, string drawTime, string resultValue)
    {
        if (drawDate == default) return "Draw date is required.";
        if (string.IsNullOrWhiteSpace(drawTime) || !ValidDrawTimes.Contains(drawTime))
            return "Draw time must be one of: 1 PM, 6 PM, 8 PM.";
        if (string.IsNullOrWhiteSpace(resultValue) || !resultValue.All(char.IsDigit))
            return "Result value is required and must be numeric.";
        if (resultValue.Length > 10) return "Result value is too long.";
        return null;
    }
}
