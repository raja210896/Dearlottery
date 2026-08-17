using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Analysis;

/// <summary>
/// Transparent, modular scoring engine. FinalScore = weighted sum of five sub-scores, normalized to 0-100.
/// This is a statistical weighting model, not a predictor of random lottery outcomes.
/// </summary>
public class CandidateScoringService : ICandidateScoringService
{
    private readonly AppDbContext _db;
    private readonly ScoringWeights _weights;

    public CandidateScoringService(AppDbContext db, IOptions<ScoringWeights> weights)
    {
        _db = db;
        _weights = weights.Value;
    }

    private const int RecentWindow = 30;

    public async Task<CandidateResponse> GetCandidatesAsync(
        string? drawTime, int digitLength, DateOnly? from, DateOnly? to, int count, CancellationToken ct = default)
    {
        digitLength = Math.Clamp(digitLength, 1, 5); // 5 = exact/full number
        count = Math.Clamp(count, 1, 50);

        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);
        if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);

        var results = await query.OrderByDescending(r => r.DrawDate).ToListAsync(ct);
        if (results.Count == 0)
        {
            return new CandidateResponse(); // no data yet
        }

        var values = results.Select(r => LastDigits(r.ResultValue, digitLength)).ToList();
        var freqMap = values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
        var maxFreq = freqMap.Values.Max();
        var recentFreqMap = values.Take(RecentWindow).GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());

        // drawsSince: index of first occurrence in the (already date-descending) list
        var drawsSince = new Dictionary<string, int>();
        for (var i = 0; i < values.Count; i++)
        {
            if (!drawsSince.ContainsKey(values[i])) drawsSince[values[i]] = i;
        }
        var maxDrawsSince = Math.Max(1, drawsSince.Values.DefaultIfEmpty(0).Max());

        var digitSums = values.Select(DigitSum).ToList();
        var digitSumFreq = digitSums.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        var maxDigitSumFreq = digitSumFreq.Values.DefaultIfEmpty(1).Max();

        var repeatingCount = values.Count(v => v.Length > 1 && v.Distinct().Count() < v.Length);
        var repeatingRate = (double)repeatingCount / values.Count;

        var oddCount = values.Count(v => int.TryParse(v, out var n) && n % 2 != 0);
        var oddRate = (double)oddCount / values.Count;

        var universe = (int)Math.Pow(10, digitLength);
        var candidates = new List<Candidate>();

        for (var i = 0; i < universe; i++)
        {
            var value = i.ToString().PadLeft(digitLength, '0');

            var frequencyScore = ScaleTo100(freqMap.GetValueOrDefault(value), maxFreq);
            var recencyScore = ScaleTo100(drawsSince.GetValueOrDefault(value, maxDrawsSince), maxDrawsSince);

            var sum = DigitSum(value);
            var digitScore = ScaleTo100(digitSumFreq.GetValueOrDefault(sum), maxDigitSumFreq);

            var isRepeating = value.Length > 1 && value.Distinct().Count() < value.Length;
            var repeatScore = (isRepeating ? repeatingRate : 1 - repeatingRate) * 100;

            var isOdd = int.Parse(value) % 2 != 0;
            var patternScore = (isOdd ? oddRate : 1 - oddRate) * 100;

            var breakdown = new ScoreBreakdown
            {
                FrequencyScore = Math.Round(frequencyScore, 1),
                RecencyScore = Math.Round(recencyScore, 1),
                DigitScore = Math.Round(digitScore, 1),
                RepeatScore = Math.Round(repeatScore, 1),
                PatternScore = Math.Round(patternScore, 1)
            };

            var totalWeight = _weights.Frequency + _weights.Recency + _weights.Digit + _weights.Repeat + _weights.Pattern;
            if (totalWeight <= 0) totalWeight = 1;

            var finalScore =
                (breakdown.FrequencyScore * _weights.Frequency +
                 breakdown.RecencyScore * _weights.Recency +
                 breakdown.DigitScore * _weights.Digit +
                 breakdown.RepeatScore * _weights.Repeat +
                 breakdown.PatternScore * _weights.Pattern) / totalWeight;

            candidates.Add(new Candidate
            {
                Value = value,
                ModelScore = Math.Round(finalScore, 1),
                Breakdown = breakdown,
                HistoricalFrequency = freqMap.GetValueOrDefault(value),
                RecentFrequency = recentFreqMap.GetValueOrDefault(value)
                // Reason is filled in below, only for the small top-N slice actually returned —
                // building it for the whole (possibly 100,000-value) universe is unnecessary work.
            });
        }

        var top = candidates.OrderByDescending(c => c.ModelScore).Take(count).ToList();
        foreach (var c in top)
        {
            var isRepeating = c.Value.Length > 1 && c.Value.Distinct().Count() < c.Value.Length;
            var isOdd = int.Parse(c.Value) % 2 != 0;
            c.Reason = BuildReason(c.Breakdown, c.HistoricalFrequency, c.RecentFrequency, isRepeating, isOdd);
        }

        return new CandidateResponse
        {
            DrawTime = string.IsNullOrWhiteSpace(drawTime) ? "All draws" : drawTime,
            Candidates = top
        };
    }

    private static string BuildReason(ScoreBreakdown b, int historicalFrequency, int recentFrequency, bool isRepeating, bool isOdd)
    {
        var parts = new List<string>();
        if (b.FrequencyScore >= 70) parts.Add($"appeared {historicalFrequency}x historically");
        if (b.RecencyScore >= 70) parts.Add("overdue — not seen in recent draws");
        else if (recentFrequency > 0) parts.Add($"seen {recentFrequency}x in the last {RecentWindow} draws");
        if (b.RepeatScore >= 70) parts.Add(isRepeating ? "matches the common repeated-digit pattern" : "matches the common non-repeating pattern");
        if (b.PatternScore >= 70) parts.Add(isOdd ? "matches the common odd pattern" : "matches the common even pattern");
        return parts.Count > 0 ? string.Join("; ", parts) : "Balanced across historical factors.";
    }

    private static double ScaleTo100(int value, int max) => max <= 0 ? 0 : (double)value / max * 100;

    private static int DigitSum(string value) => value.Sum(c => c - '0');

    private static string LastDigits(string value, int n) =>
        value.Length <= n ? value.PadLeft(n, '0') : value[^n..];
}
