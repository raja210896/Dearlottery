using System.Text.Json;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Analysis;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _db;

    public AnalysisService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FrequencySnapshot> GetFrequencyAsync(string? drawTime, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var results = await QueryResults(drawTime, from, to).ToListAsync(ct);

        var full = CountBy(results, r => r.ResultValue);
        var last1 = CountBy(results, r => LastDigits(r.ResultValue, 1));
        var last2 = CountBy(results, r => LastDigits(r.ResultValue, 2));
        var last3 = CountBy(results, r => LastDigits(r.ResultValue, 3));

        var ordered2 = last2.OrderByDescending(e => e.Count).ToList();

        return new FrequencySnapshot
        {
            FullNumberFrequency = full.OrderByDescending(e => e.Count).ToList(),
            LastDigitFrequency = last1.OrderByDescending(e => e.Count).ToList(),
            Last2DigitFrequency = ordered2,
            Last3DigitFrequency = last3.OrderByDescending(e => e.Count).ToList(),
            HotNumbers = ordered2.Take(10).ToList(),
            ColdNumbers = ordered2.OrderBy(e => e.Count).Take(10).ToList(),
            SampleSize = results.Count
        };
    }

    public async Task<List<RecencyEntry>> GetRecencyAsync(string? drawTime, int digitLength, int recentWindow, CancellationToken ct = default)
    {
        var results = await QueryResults(drawTime, null, null).ToListAsync(ct);
        if (results.Count == 0) return new List<RecencyEntry>();

        var ordered = results.OrderByDescending(r => r.DrawDate).ToList();
        var recent = ordered.Take(Math.Max(1, recentWindow)).ToList();

        var groups = ordered
            .Select(r => new { Value = LastDigits(r.ResultValue, digitLength), r.DrawDate })
            .GroupBy(x => x.Value);

        var entries = new List<RecencyEntry>();
        foreach (var g in groups)
        {
            var lastSeen = g.Max(x => x.DrawDate);
            // ordered is most-recent-first; index of the first matching draw = how many draws have happened since
            var drawsSince = ordered.FindIndex(r => LastDigits(r.ResultValue, digitLength) == g.Key);
            var recentCount = recent.Count(r => LastDigits(r.ResultValue, digitLength) == g.Key);

            entries.Add(new RecencyEntry
            {
                Value = g.Key,
                LastAppearance = lastSeen,
                DrawsSinceAppearance = drawsSince,
                RecentFrequency = recentCount
            });
        }

        return entries.OrderBy(e => e.DrawsSinceAppearance).ToList();
    }

    public async Task<PatternStats> GetPatternStatsAsync(string? drawTime, int digitLength, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var results = await QueryResults(drawTime, from, to).OrderBy(r => r.DrawDate).ToListAsync(ct);
        var stats = new PatternStats();

        foreach (var r in results)
        {
            var val = LastDigits(r.ResultValue, digitLength);
            if (!int.TryParse(val, out var num)) continue;

            if (num % 2 == 0) stats.EvenCount++; else stats.OddCount++;

            var digitSum = val.Sum(c => c - '0');
            stats.DigitSumDistribution[digitSum] = stats.DigitSumDistribution.GetValueOrDefault(digitSum) + 1;

            if (val.Length > 1 && val.Distinct().Count() < val.Length) stats.RepeatedDigitCount++;
        }

        // recent repeats: same last-N-digit value appearing more than once within the queried window
        var byValue = results
            .Select(r => new { Value = LastDigits(r.ResultValue, digitLength), r.DrawDate })
            .GroupBy(x => x.Value)
            .Where(g => g.Count() > 1);

        foreach (var g in byValue)
        {
            var dates = g.Select(x => x.DrawDate).OrderBy(d => d).ToList();
            for (var i = 1; i < dates.Count; i++)
            {
                stats.RecentRepeats.Add(new RecentRepeat
                {
                    Value = g.Key,
                    FirstDate = dates[i - 1],
                    SecondDate = dates[i],
                    DrawsApart = dates[i].DayNumber - dates[i - 1].DayNumber
                });
            }
        }
        stats.RecentRepeats = stats.RecentRepeats.OrderByDescending(r => r.SecondDate).Take(20).ToList();

        return stats;
    }

    public async Task<AnalysisOverview> GetOverviewAsync(string? drawTime, CancellationToken ct = default)
    {
        const string snapshotType = "overview";
        var key = drawTime ?? "all";

        var cached = await _db.AnalysisSnapshots
            .Where(s => s.SnapshotType == snapshotType && s.DrawTime == key && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (cached is not null)
        {
            var deserialized = JsonSerializer.Deserialize<AnalysisOverview>(cached.DataJson);
            if (deserialized is not null) return deserialized;
        }

        var overview = new AnalysisOverview
        {
            Frequency = await GetFrequencyAsync(drawTime, null, null, ct),
            Recency = await GetRecencyAsync(drawTime, 2, 30, ct),
            Patterns = await GetPatternStatsAsync(drawTime, 2, null, null, ct)
        };

        _db.AnalysisSnapshots.Add(new AnalysisSnapshot
        {
            DrawTime = key,
            SnapshotType = snapshotType,
            DataJson = JsonSerializer.Serialize(overview),
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(6)
        });
        await _db.SaveChangesAsync(ct);

        return overview;
    }

    public async Task<DigitAnalysis> GetDigitAnalysisAsync(string? drawTime, DateOnly? from, DateOnly? to, int recentWindow, CancellationToken ct = default)
    {
        var results = await QueryResults(drawTime, from, to).OrderByDescending(r => r.DrawDate).ToListAsync(ct);
        var analysis = new DigitAnalysis { SampleSize = results.Count };
        if (results.Count == 0) return analysis;

        // Digit frequency 0-9 across all positions of every number.
        var digitCounts = new int[10];
        var maxLength = results.Max(r => r.ResultValue.Length);
        var positionCounts = Enumerable.Range(0, maxLength).Select(_ => new int[10]).ToArray();
        var pairCounts = new Dictionary<string, int>();

        foreach (var r in results)
        {
            var value = r.ResultValue;
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i])) continue;
                var d = value[i] - '0';
                digitCounts[d]++;
                positionCounts[i][d]++;
            }

            for (var i = 0; i < value.Length - 1; i++)
            {
                if (!char.IsDigit(value[i]) || !char.IsDigit(value[i + 1])) continue;
                var pair = value.Substring(i, 2);
                pairCounts[pair] = pairCounts.GetValueOrDefault(pair) + 1;
            }
        }

        analysis.DigitFrequency = Enumerable.Range(0, 10)
            .Select(d => new FrequencyEntry { Value = d.ToString(), Count = digitCounts[d] })
            .OrderByDescending(e => e.Count)
            .ToList();
        analysis.HotDigits = analysis.DigitFrequency.Take(3).ToList();
        analysis.ColdDigits = analysis.DigitFrequency.OrderBy(e => e.Count).Take(3).ToList();

        analysis.PositionFrequency = Enumerable.Range(0, maxLength)
            .Select(i => new PositionFrequency
            {
                Position = i + 1,
                Digits = Enumerable.Range(0, 10)
                    .Select(d => new FrequencyEntry { Value = d.ToString(), Count = positionCounts[i][d] })
                    .OrderByDescending(e => e.Count)
                    .ToList()
            })
            .ToList();

        analysis.DigitPairFrequency = pairCounts
            .Select(kv => new FrequencyEntry { Value = kv.Key, Count = kv.Value })
            .OrderByDescending(e => e.Count)
            .Take(20)
            .ToList();

        // Recent (last N draws) vs full-history frequency, at the last-2-digit level.
        var recent = results.Take(Math.Max(1, recentWindow)).ToList();
        var historicalLast2 = CountBy(results, r => LastDigits(r.ResultValue, 2)).ToDictionary(e => e.Value, e => e.Count);
        var recentLast2 = CountBy(recent, r => LastDigits(r.ResultValue, 2)).ToDictionary(e => e.Value, e => e.Count);
        analysis.RecentVsHistorical = historicalLast2.Keys.Union(recentLast2.Keys)
            .Select(v => new RecentVsHistoricalEntry
            {
                Value = v,
                HistoricalCount = historicalLast2.GetValueOrDefault(v),
                RecentCount = recentLast2.GetValueOrDefault(v)
            })
            .OrderByDescending(e => e.RecentCount)
            .ThenByDescending(e => e.HistoricalCount)
            .Take(20)
            .ToList();

        return analysis;
    }

    private static readonly string[] AllDrawTimes = { "1 PM", "6 PM", "8 PM" };

    /// <summary>
    /// Read-only, additive analysis — never touches the Multi-Factor candidate scoring model or
    /// saved prediction records. For each draw time: the exact result from this date one year ago
    /// (null, never guessed, if no such record exists) and a frequency ranking of results from this
    /// calendar month across every year on record.
    /// </summary>
    public async Task<SeasonalPattern> GetSeasonalPatternAsync(DateOnly targetDate, int digitLength, int topN, CancellationToken ct = default)
    {
        var lastYearDate = targetDate.AddYears(-1);
        var pattern = new SeasonalPattern { TargetDate = targetDate };

        foreach (var drawTime in AllDrawTimes)
        {
            var results = await QueryResults(drawTime, null, null).ToListAsync(ct);

            var sameDateLastYear = results.FirstOrDefault(r => r.DrawDate == lastYearDate);
            var monthResults = results.Where(r => r.DrawDate.Month == targetDate.Month).ToList();
            var monthFrequency = CountBy(monthResults, r => LastDigits(r.ResultValue, digitLength))
                .OrderByDescending(e => e.Count)
                .Take(Math.Max(1, topN))
                .ToList();

            pattern.Draws.Add(new SeasonalDrawPrediction
            {
                DrawTime = drawTime,
                SameDateLastYear = lastYearDate,
                SameDateLastYearValue = sameDateLastYear?.ResultValue,
                CurrentMonthFrequency = monthFrequency,
                CurrentMonthSampleSize = monthResults.Count
            });
        }

        return pattern;
    }

    private IQueryable<LotteryResult> QueryResults(string? drawTime, DateOnly? from, DateOnly? to)
    {
        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);
        if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);
        return query;
    }

    private static List<FrequencyEntry> CountBy(List<LotteryResult> results, Func<LotteryResult, string> selector) =>
        results.GroupBy(selector)
            .Select(g => new FrequencyEntry { Value = g.Key, Count = g.Count() })
            .ToList();

    private static string LastDigits(string value, int n) =>
        value.Length <= n ? value.PadLeft(n, '0') : value[^n..];
}
