using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Analysis;

/// <summary>
/// Runs a historical simulation: for each test draw, candidates are computed using only
/// data strictly before that draw's date (no future-data leakage), then checked against
/// the actual result. Compared against a random-baseline hit rate.
/// </summary>
public class BacktestService : IBacktestService
{
    private readonly AppDbContext _db;
    private readonly ICandidateScoringService _scoring;

    public BacktestService(AppDbContext db, ICandidateScoringService scoring)
    {
        _db = db;
        _scoring = scoring;
    }

    public async Task<BacktestResponse> RunAsync(
        string? drawTime, int digitLength, int drawCount, int candidateCount,
        DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        digitLength = Math.Clamp(digitLength, 1, 5); // 5 = exact/full number
        candidateCount = Math.Clamp(candidateCount, 1, 50);
        drawCount = Math.Clamp(drawCount, 1, 500);

        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);
        if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);

        var all = await query.OrderBy(r => r.DrawDate).ToListAsync(ct);
        var testSet = all.TakeLast(drawCount).ToList();

        var response = new BacktestResponse { TotalTested = testSet.Count, DrawsTested = testSet.Count };
        if (testSet.Count == 0) return response;

        // Fetch enough candidates to cover Top-10 even when the requested candidateCount is smaller;
        // ranking is unaffected — GetCandidatesAsync always ranks the full digit universe first (unchanged algorithm).
        var fetchCount = Math.Max(candidateCount, 10);

        foreach (var draw in testSet)
        {
            var cutoff = draw.DrawDate.AddDays(-1);
            var candidates = await _scoring.GetCandidatesAsync(
                drawTime, digitLength, null, cutoff, fetchCount, ct);

            if (candidates.Candidates.Count == 0) continue; // insufficient history yet

            var actual = LastDigits(draw.ResultValue, digitLength);
            var ranked = candidates.Candidates;
            var hit = ranked.Take(candidateCount).Any(c => c.Value == actual);
            var top1 = ranked.Count > 0 && ranked[0].Value == actual;
            var top5 = ranked.Take(5).Any(c => c.Value == actual);
            var top10 = ranked.Take(10).Any(c => c.Value == actual);

            if (hit) response.Hits++;
            if (top1) response.Top1Matches++;
            if (top5) response.Top5Matches++;
            if (top10) response.Top10Matches++;

            response.Draws.Add(new BacktestDrawResult
            {
                DrawDate = draw.DrawDate,
                ActualValue = actual,
                Hit = hit,
                TopScore = ranked.Max(c => c.ModelScore),
                Top1 = top1,
                Top5 = top5,
                Top10 = top10
            });
        }

        response.DrawsTested = response.Draws.Count;
        response.ModelHitRate = response.DrawsTested == 0 ? 0 : Math.Round((double)response.Hits / response.DrawsTested, 4);
        response.Top1MatchRate = response.DrawsTested == 0 ? 0 : Math.Round((double)response.Top1Matches / response.DrawsTested, 4);
        response.Top5MatchRate = response.DrawsTested == 0 ? 0 : Math.Round((double)response.Top5Matches / response.DrawsTested, 4);
        response.Top10MatchRate = response.DrawsTested == 0 ? 0 : Math.Round((double)response.Top10Matches / response.DrawsTested, 4);

        var universe = Math.Pow(10, digitLength);
        response.RandomBaselineRate = Math.Round(candidateCount / universe, 4);
        response.ModelVsRandomDifference = Math.Round(response.ModelHitRate - response.RandomBaselineRate, 4);

        return response;
    }

    public async Task<DataQualitySummary> GetDataQualityAsync(string? drawTime, CancellationToken ct = default)
    {
        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);

        var summary = new DataQualitySummary { TotalDraws = await query.CountAsync(ct) };
        if (summary.TotalDraws == 0) return summary;

        summary.EarliestDate = await query.MinAsync(r => r.DrawDate, ct);
        summary.LatestDate = await query.MaxAsync(r => r.DrawDate, ct);
        summary.CountsByDrawTime = await query
            .GroupBy(r => r.DrawTime)
            .Select(g => new DrawTimeCount { DrawTime = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var expectedDrawTimes = string.IsNullOrWhiteSpace(drawTime) ? new[] { "1 PM", "6 PM", "8 PM" } : new[] { drawTime };
        var present = await query.Select(r => new { r.DrawDate, r.DrawTime }).ToListAsync(ct);
        var presentSet = present.Select(p => (p.DrawDate, p.DrawTime)).ToHashSet();

        var missingDates = new List<DateOnly>();
        for (var d = summary.EarliestDate.Value; d <= summary.LatestDate.Value; d = d.AddDays(1))
        {
            foreach (var dt in expectedDrawTimes)
            {
                if (presentSet.Contains((d, dt))) continue;
                summary.MissingSlotCount++;
                if (missingDates.Count < 20 && !missingDates.Contains(d)) missingDates.Add(d);
            }
        }
        summary.SampleMissingDates = missingDates;

        return summary;
    }

    public async Task<MultiDigitBacktestSummary> RunMultiAsync(
        string? drawTime, int drawCount, int candidateCount, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        return new MultiDigitBacktestSummary
        {
            Exact = await RunAsync(drawTime, 5, drawCount, candidateCount, from, to, ct),
            Last2 = await RunAsync(drawTime, 2, drawCount, candidateCount, from, to, ct),
            Last3 = await RunAsync(drawTime, 3, drawCount, candidateCount, from, to, ct)
        };
    }

    private static string LastDigits(string value, int n) =>
        value.Length <= n ? value.PadLeft(n, '0') : value[^n..];
}
