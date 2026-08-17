using LotteryAnalytics.Api.Services.Analysis;
using Microsoft.Extensions.Options;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class BacktestServiceTests
{
    [Fact]
    public async Task RunAsync_ComputesHitRateAgainstRandomBaseline()
    {
        var db = TestDbContextFactory.Create();
        var values = Enumerable.Range(0, 50).Select(i => (i * 13 % 100).ToString("D2")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunAsync("1 PM", 2, 20, 10, null, null);

        Assert.True(result.DrawsTested > 0);
        Assert.Equal(0.1, result.RandomBaselineRate, 3); // 10 candidates / 100 possible 2-digit values
        Assert.InRange(result.ModelHitRate, 0, 1);
        Assert.Equal(result.Hits, result.Draws.Count(d => d.Hit));
    }

    [Fact]
    public async Task RunAsync_DoesNotLeakFutureData()
    {
        // Only one result exists total; backtesting it must not "see" itself when scoring.
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", new[] { "42" }, new DateOnly(2026, 1, 1));

        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunAsync("1 PM", 2, 30, 10, null, null);

        // No prior history exists before the only draw, so it must be skipped (not tested).
        Assert.Equal(0, result.DrawsTested);
    }

    [Fact]
    public async Task RunAsync_NoData_ReturnsZeroedResponse()
    {
        var db = TestDbContextFactory.Create();
        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunAsync("1 PM", 2, 30, 10, null, null);

        Assert.Equal(0, result.DrawsTested);
        Assert.Equal(0, result.Hits);
    }

    [Fact]
    public async Task RunAsync_ComputesTop1Top5Top10MatchRates()
    {
        var db = TestDbContextFactory.Create();
        var values = Enumerable.Range(0, 50).Select(i => (i * 13 % 100).ToString("D2")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunAsync("1 PM", 2, 20, 10, null, null);

        Assert.InRange(result.Top1MatchRate, 0, 1);
        Assert.InRange(result.Top5MatchRate, 0, 1);
        Assert.InRange(result.Top10MatchRate, 0, 1);
        // Top-1 candidates are a subset of Top-5, which are a subset of Top-10 — rates must be monotonic.
        Assert.True(result.Top1MatchRate <= result.Top5MatchRate);
        Assert.True(result.Top5MatchRate <= result.Top10MatchRate);
        Assert.Equal(result.Top1Matches, result.Draws.Count(d => d.Top1));
        Assert.Equal(result.Top5Matches, result.Draws.Count(d => d.Top5));
        Assert.Equal(result.Top10Matches, result.Draws.Count(d => d.Top10));
    }

    [Fact]
    public async Task RunAsync_EvaluatesDrawsInChronologicalOrder()
    {
        var db = TestDbContextFactory.Create();
        // Seed out of chronological insertion order; the service must still sort by DrawDate.
        var values = new[] { "10", "20", "30" };
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 3, 1));

        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunAsync("1 PM", 2, 10, 10, null, null);

        var dates = result.Draws.Select(d => d.DrawDate).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);
    }

    [Fact]
    public async Task RunMultiAsync_ReturnsExactLast2AndLast3Results()
    {
        var db = TestDbContextFactory.Create();
        var values = Enumerable.Range(0, 40).Select(i => (10000 + i * 137 % 90000).ToString("D5")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var backtest = new BacktestService(db, scoring);

        var result = await backtest.RunMultiAsync("1 PM", 20, 10, null, null);

        Assert.True(result.Exact.DrawsTested > 0);
        Assert.True(result.Last2.DrawsTested > 0);
        Assert.True(result.Last3.DrawsTested > 0);
        Assert.All(result.Exact.Draws, d => Assert.Equal(5, d.ActualValue.Length));
        Assert.All(result.Last2.Draws, d => Assert.Equal(2, d.ActualValue.Length));
        Assert.All(result.Last3.Draws, d => Assert.Equal(3, d.ActualValue.Length));
    }
}
