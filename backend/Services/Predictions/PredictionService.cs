using System.Text.Json;
using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Analysis;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Predictions;

/// <summary>
/// Saves a snapshot of statistical candidates for a draw, then automatically evaluates it
/// against the actual result once one is entered. Reuses the existing candidate scoring
/// service as-is — no scoring/model changes.
/// </summary>
public class PredictionService : IPredictionService
{
    private const string CurrentModelVersion = "1.0";
    private static readonly HashSet<string> ValidDrawTimes = new() { "1 PM", "6 PM", "8 PM" };

    private readonly AppDbContext _db;
    private readonly ICandidateScoringService _scoring;

    public PredictionService(AppDbContext db, ICandidateScoringService scoring)
    {
        _db = db;
        _scoring = scoring;
    }

    public async Task<PredictionSaveOutcome> SaveSnapshotAsync(DateOnly drawDate, string drawTime, int digitLength, int count, CancellationToken ct = default)
    {
        if (drawDate == default) return PredictionSaveOutcome.Fail("Draw date is required.");
        if (string.IsNullOrWhiteSpace(drawTime) || !ValidDrawTimes.Contains(drawTime))
            return PredictionSaveOutcome.Fail("Draw time must be one of: 1 PM, 6 PM, 8 PM.");
        digitLength = Math.Clamp(digitLength, 1, 3);
        count = Math.Clamp(count, 1, 50);

        var duplicate = await _db.PredictionRecords.AnyAsync(p =>
            p.DrawDate == drawDate && p.DrawTime == drawTime && p.DigitLength == digitLength && p.ModelVersion == CurrentModelVersion, ct);
        if (duplicate) return PredictionSaveOutcome.Fail("A prediction snapshot for this draw already exists.");

        // Candidates are generated only from data strictly before the draw date (no leakage).
        var candidates = await _scoring.GetCandidatesAsync(drawTime, digitLength, null, drawDate.AddDays(-1), count, ct);
        if (candidates.Candidates.Count == 0)
            return PredictionSaveOutcome.Fail("Not enough historical data to generate a prediction for this draw yet.");

        var entity = new PredictionRecord
        {
            DrawDate = drawDate,
            DrawTime = drawTime,
            DigitLength = digitLength,
            Candidates = JsonSerializer.Serialize(candidates.Candidates),
            GeneratedAt = DateTime.UtcNow,
            ModelVersion = CurrentModelVersion
        };
        _db.PredictionRecords.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return PredictionSaveOutcome.Fail("A prediction snapshot for this draw already exists.");
        }

        return new PredictionSaveOutcome { Success = true, Prediction = ToDto(entity) };
    }

    public async Task EvaluatePendingAsync(LotteryResult result, CancellationToken ct = default)
    {
        var pending = await _db.PredictionRecords
            .Where(p => !p.IsEvaluated && p.DrawDate == result.DrawDate && p.DrawTime == result.DrawTime)
            .ToListAsync(ct);
        if (pending.Count == 0) return;

        foreach (var prediction in pending)
        {
            var candidates = JsonSerializer.Deserialize<List<Candidate>>(prediction.Candidates) ?? new();
            var actual = LastDigits(result.ResultValue, prediction.DigitLength);
            var matchIndex = candidates.FindIndex(c => c.Value == actual);

            prediction.ActualResult = result.ResultValue;
            prediction.IsEvaluated = true;
            prediction.MatchFound = matchIndex >= 0;
            prediction.MatchPosition = matchIndex >= 0 ? matchIndex + 1 : null;
            prediction.EvaluatedAt = DateTime.UtcNow;
            prediction.LotteryResultId = result.Id;

            // Supplementary Exact/Last-3/Last-2 match flags, using the same no-leakage cutoff as the
            // original snapshot. Reuses the existing (unchanged) scoring service — no new algorithm.
            var cutoff = prediction.DrawDate.AddDays(-1);
            prediction.ExactMatch = await MatchesTopTenAsync(prediction.DrawTime, 5, cutoff, result.ResultValue, ct);
            prediction.Last3Match = await MatchesTopTenAsync(prediction.DrawTime, 3, cutoff, result.ResultValue, ct);
            prediction.Last2Match = await MatchesTopTenAsync(prediction.DrawTime, 2, cutoff, result.ResultValue, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool?> MatchesTopTenAsync(string drawTime, int digitLength, DateOnly cutoff, string actualResult, CancellationToken ct)
    {
        var candidates = await _scoring.GetCandidatesAsync(drawTime, digitLength, null, cutoff, 10, ct);
        if (candidates.Candidates.Count == 0) return null; // insufficient prior history at this granularity
        var actual = LastDigits(actualResult, digitLength);
        return candidates.Candidates.Any(c => c.Value == actual);
    }

    public async Task<PagedResult<PredictionHistoryDto>> GetHistoryAsync(
        DateOnly? from, DateOnly? to, string? drawTime, int? digitLength, string? matchStatus,
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.PredictionRecords.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(p => p.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.DrawDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(p => p.DrawTime == drawTime);
        if (digitLength.HasValue) query = query.Where(p => p.DigitLength == digitLength.Value);
        query = matchStatus switch
        {
            "matched" => query.Where(p => p.MatchFound == true),
            "unmatched" => query.Where(p => p.IsEvaluated && p.MatchFound == false),
            "pending" => query.Where(p => !p.IsEvaluated),
            _ => query
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.DrawDate).ThenBy(p => p.DrawTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<PredictionHistoryDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PredictionPerformanceDto> GetPerformanceAsync(string? drawTime, CancellationToken ct = default)
    {
        var query = _db.PredictionRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(p => p.DrawTime == drawTime);

        var all = await query.ToListAsync(ct);
        var evaluated = all.Where(p => p.IsEvaluated).ToList();
        var matches = evaluated.Count(p => p.MatchFound == true);

        var baselineRates = evaluated
            .Select(p => CandidateCountOf(p.Candidates) / Math.Pow(10, p.DigitLength))
            .ToList();

        return new PredictionPerformanceDto
        {
            TotalPredictions = all.Count,
            EvaluatedPredictions = evaluated.Count,
            Matches = matches,
            MatchRate = evaluated.Count == 0 ? 0 : Math.Round((double)matches / evaluated.Count, 4),
            RandomBaselineRate = baselineRates.Count == 0 ? 0 : Math.Round(baselineRates.Average(), 4),
            RecentPerformance = evaluated
                .OrderByDescending(p => p.EvaluatedAt)
                .Take(10)
                .Select(p => new RecentPredictionOutcome { DrawDate = p.DrawDate, DrawTime = p.DrawTime, MatchFound = p.MatchFound == true })
                .ToList()
        };
    }

    private static int CandidateCountOf(string candidatesJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Candidate>>(candidatesJson)?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static PredictionHistoryDto ToDto(PredictionRecord p) => new()
    {
        Id = p.Id,
        DrawDate = p.DrawDate,
        DrawTime = p.DrawTime,
        DigitLength = p.DigitLength,
        Candidates = JsonSerializer.Deserialize<List<Candidate>>(p.Candidates) ?? new(),
        GeneratedAt = p.GeneratedAt,
        ActualResult = p.ActualResult,
        IsEvaluated = p.IsEvaluated,
        MatchFound = p.MatchFound,
        MatchPosition = p.MatchPosition,
        EvaluatedAt = p.EvaluatedAt,
        ExactMatch = p.ExactMatch,
        Last3Match = p.Last3Match,
        Last2Match = p.Last2Match
    };

    private static string LastDigits(string value, int n) =>
        value.Length <= n ? value.PadLeft(n, '0') : value[^n..];
}
