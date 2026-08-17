using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Analysis;

/// <summary>
/// Read-only model comparison. Reuses the existing (unchanged) Multi-Factor scoring service
/// for the "current model" leg; the frequency-only, recency-only and random baselines are
/// simple, independent ranking rules implemented here — they do not modify or call into
/// CandidateScoringService's weighted formula.
/// </summary>
public class ModelEvaluationService : IModelEvaluationService
{
    private static readonly string[] DrawTimes = { "1 PM", "6 PM", "8 PM" };
    private static readonly (int DigitLength, string Label)[] Granularities =
    {
        (5, "Exact"), (3, "Last3"), (2, "Last2")
    };

    private readonly AppDbContext _db;
    private readonly ICandidateScoringService _scoring;

    public ModelEvaluationService(AppDbContext db, ICandidateScoringService scoring)
    {
        _db = db;
        _scoring = scoring;
    }

    public async Task<ModelComparisonResponse> CompareModelsAsync(
        int drawCount, int candidateCount, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        drawCount = Math.Clamp(drawCount, 1, 200);
        candidateCount = Math.Clamp(candidateCount, 1, 50);

        var response = new ModelComparisonResponse();

        foreach (var drawTime in DrawTimes)
        {
            var query = _db.LotteryResults.AsNoTracking().Where(r => r.DrawTime == drawTime);
            if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
            if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);

            var all = await query.OrderBy(r => r.DrawDate).ToListAsync(ct);
            var testSet = all.TakeLast(drawCount).ToList();

            var comparison = new DrawTimeModelComparison { DrawTime = drawTime };

            foreach (var (digitLength, label) in Granularities)
            {
                var multiFactor = await EvaluateMultiFactorAsync(drawTime, testSet, digitLength, candidateCount, ct);
                var frequencyOnly = EvaluateBaseline(all, testSet, digitLength, candidateCount, RankByFrequency);
                var recencyOnly = EvaluateBaseline(all, testSet, digitLength, candidateCount, RankByRecency);
                var random = EvaluateRandomBaseline(digitLength, candidateCount, multiFactor.DrawsTested);

                Assign(comparison.MultiFactor, label, multiFactor);
                Assign(comparison.FrequencyOnly, label, frequencyOnly);
                Assign(comparison.RecencyOnly, label, recencyOnly);
                Assign(comparison.Random, label, random);
            }

            response.ByDrawTime.Add(comparison);
        }

        return response;
    }

    private static void Assign(ModelDigitResults target, string label, ModelHitRateResult value)
    {
        switch (label)
        {
            case "Exact": target.Exact = value; break;
            case "Last3": target.Last3 = value; break;
            case "Last2": target.Last2 = value; break;
        }
    }

    /// <summary>Current Multi-Factor model — delegates to the existing, unchanged scoring service.</summary>
    private async Task<ModelHitRateResult> EvaluateMultiFactorAsync(
        string drawTime, List<LotteryResult> testSet, int digitLength, int candidateCount, CancellationToken ct)
    {
        var result = new ModelHitRateResult();
        foreach (var draw in testSet)
        {
            var cutoff = draw.DrawDate.AddDays(-1);
            var candidates = await _scoring.GetCandidatesAsync(drawTime, digitLength, null, cutoff, candidateCount, ct);
            if (candidates.Candidates.Count == 0) continue; // insufficient prior history

            result.DrawsTested++;
            var actual = LastDigits(draw.ResultValue, digitLength);
            if (candidates.Candidates.Any(c => c.Value == actual)) result.Hits++;
        }
        result.HitRate = result.DrawsTested == 0 ? 0 : Math.Round((double)result.Hits / result.DrawsTested, 4);
        return result;
    }

    /// <summary>Frequency-only / recency-only baseline evaluator — ranks using only in-memory training data before each test draw's cutoff.</summary>
    private static ModelHitRateResult EvaluateBaseline(
        List<LotteryResult> all, List<LotteryResult> testSet, int digitLength, int candidateCount,
        Func<List<string>, int, List<string>> rank)
    {
        var result = new ModelHitRateResult();
        foreach (var draw in testSet)
        {
            var cutoff = draw.DrawDate.AddDays(-1);
            var trainingValues = all
                .Where(r => r.DrawDate <= cutoff)
                .Select(r => LastDigits(r.ResultValue, digitLength))
                .ToList();
            if (trainingValues.Count == 0) continue; // insufficient prior history — matches Multi-Factor's own skip rule

            result.DrawsTested++;
            var ranked = rank(trainingValues, candidateCount);
            var actual = LastDigits(draw.ResultValue, digitLength);
            if (ranked.Contains(actual)) result.Hits++;
        }
        result.HitRate = result.DrawsTested == 0 ? 0 : Math.Round((double)result.Hits / result.DrawsTested, 4);
        return result;
    }

    private static List<string> RankByFrequency(List<string> trainingValues, int candidateCount) =>
        trainingValues.GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .Take(candidateCount)
            .ToList();

    private static List<string> RankByRecency(List<string> trainingValuesOldestFirst, int candidateCount)
    {
        // trainingValuesOldestFirst is ascending by date. Walk from the end (most recent) backwards;
        // a value's first hit in that walk is its most-recent occurrence. Rank 0 = most recent,
        // so the largest rank among first-occurrences = least recently seen = most overdue.
        var recencyRank = new Dictionary<string, int>();
        for (var i = trainingValuesOldestFirst.Count - 1; i >= 0; i--)
        {
            var value = trainingValuesOldestFirst[i];
            var rank = trainingValuesOldestFirst.Count - 1 - i;
            if (!recencyRank.ContainsKey(value)) recencyRank[value] = rank;
        }
        return recencyRank
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Select(kv => kv.Key)
            .Take(candidateCount)
            .ToList();
    }

    /// <summary>Analytical baseline (candidateCount / universe) — never generates or claims an actual random draw.</summary>
    private static ModelHitRateResult EvaluateRandomBaseline(int digitLength, int candidateCount, int drawsTested)
    {
        var universe = Math.Pow(10, digitLength);
        return new ModelHitRateResult
        {
            DrawsTested = drawsTested,
            Hits = -1, // not simulated — analytical rate only
            HitRate = Math.Round(candidateCount / universe, 6)
        };
    }

    private static string LastDigits(string value, int n) =>
        value.Length <= n ? value.PadLeft(n, '0') : value[^n..];
}
