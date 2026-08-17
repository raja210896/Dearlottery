using LotteryAnalytics.Api.Services.Analysis;
using Microsoft.Extensions.Options;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class ModelEvaluationServiceTests
{
    private static ModelEvaluationService BuildService(Api.Data.AppDbContext db) =>
        new(db, new CandidateScoringService(db, Options.Create(new ScoringWeights())));

    private static string[] SampleValues(int n) =>
        Enumerable.Range(0, n).Select(i => (10000 + i * 137 % 90000).ToString("D5")).ToArray();

    [Fact]
    public async Task CompareModelsAsync_EvaluatesAllFourModelsForAllThreeDrawTimes()
    {
        var db = TestDbContextFactory.Create();
        var start = new DateOnly(2026, 1, 1);
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", SampleValues(40), start);
        await TestDbContextFactory.SeedResultsAsync(db, "6 PM", SampleValues(40), start);
        await TestDbContextFactory.SeedResultsAsync(db, "8 PM", SampleValues(40), start);

        var service = BuildService(db);
        var result = await service.CompareModelsAsync(8, 10, null, null);

        Assert.Equal(new[] { "1 PM", "6 PM", "8 PM" }, result.ByDrawTime.Select(d => d.DrawTime));
        foreach (var d in result.ByDrawTime)
        {
            Assert.True(d.MultiFactor.Exact.DrawsTested > 0);
            Assert.True(d.FrequencyOnly.Last2.DrawsTested > 0);
            Assert.True(d.RecencyOnly.Last2.DrawsTested > 0);
            Assert.True(d.Random.Last2.DrawsTested > 0);
        }
    }

    [Fact]
    public async Task CompareModelsAsync_RandomBaselineIsAnalyticalNotSimulated()
    {
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", SampleValues(30), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "6 PM", SampleValues(30), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "8 PM", SampleValues(30), new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.CompareModelsAsync(5, 10, null, null);

        var onePm = result.ByDrawTime.Single(d => d.DrawTime == "1 PM");
        Assert.Equal(-1, onePm.Random.Last2.Hits); // sentinel: not simulated
        Assert.Equal(0.1, onePm.Random.Last2.HitRate, 3); // 10 candidates / 100 possible 2-digit values
        Assert.Equal(0.01, onePm.Random.Last3.HitRate, 4); // 10 / 1000
        Assert.Equal(0.0001, onePm.Random.Exact.HitRate, 5); // 10 / 100000
    }

    [Fact]
    public async Task CompareModelsAsync_UsesIdenticalNoLeakageTestWindowAcrossModels()
    {
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", SampleValues(25), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "6 PM", SampleValues(25), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "8 PM", SampleValues(25), new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.CompareModelsAsync(8, 10, null, null);

        var onePm = result.ByDrawTime.Single(d => d.DrawTime == "1 PM");
        // Every model's DrawsTested reflects the same chronological window (first draw excluded — no prior history).
        Assert.Equal(onePm.MultiFactor.Last2.DrawsTested, onePm.FrequencyOnly.Last2.DrawsTested);
        Assert.Equal(onePm.MultiFactor.Last2.DrawsTested, onePm.RecencyOnly.Last2.DrawsTested);
    }

    [Fact]
    public async Task CompareModelsAsync_FrequencyOnlyRanksMoreFrequentValueHigher()
    {
        var db = TestDbContextFactory.Create();
        // "25" appears far more often than anything else in the training window.
        var values = new List<string> { "25", "25", "25", "25", "25", "13", "47", "88", "02", "91", "10", "20" };
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values.ToArray(), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "6 PM", values.ToArray(), new DateOnly(2026, 1, 1));
        await TestDbContextFactory.SeedResultsAsync(db, "8 PM", values.ToArray(), new DateOnly(2026, 1, 1));
        // One more draw after, whose actual result is "25" — the frequency-only baseline should catch it.
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", new[] { "25" }, new DateOnly(2026, 1, 13));

        var service = BuildService(db);
        var result = await service.CompareModelsAsync(1, 1, null, null);

        var onePm = result.ByDrawTime.Single(d => d.DrawTime == "1 PM");
        Assert.Equal(1, onePm.FrequencyOnly.Last2.Hits); // top-1 by frequency ("25") matches the actual result
    }
}
