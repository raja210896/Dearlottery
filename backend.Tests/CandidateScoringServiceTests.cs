using LotteryAnalytics.Api.Services.Analysis;
using Microsoft.Extensions.Options;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class CandidateScoringServiceTests
{
    private static CandidateScoringService BuildService(Api.Data.AppDbContext db, ScoringWeights? weights = null) =>
        new(db, Options.Create(weights ?? new ScoringWeights()));

    [Fact]
    public async Task GetCandidatesAsync_ReturnsRequestedCount_WithScoresInRange()
    {
        var db = TestDbContextFactory.Create();
        var values = Enumerable.Range(0, 40).Select(i => (i * 7 % 100).ToString("D2")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.GetCandidatesAsync("1 PM", 2, null, null, 10);

        Assert.Equal(10, result.Candidates.Count);
        Assert.All(result.Candidates, c => Assert.InRange(c.ModelScore, 0, 100));
        // Candidates must be unique values
        Assert.Equal(result.Candidates.Count, result.Candidates.Select(c => c.Value).Distinct().Count());
        // Sorted descending by score
        Assert.Equal(result.Candidates.OrderByDescending(c => c.ModelScore).Select(c => c.Value), result.Candidates.Select(c => c.Value));
    }

    [Fact]
    public async Task GetCandidatesAsync_NoHistory_ReturnsEmpty()
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);

        var result = await service.GetCandidatesAsync("1 PM", 2, null, null, 10);

        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task GetCandidatesAsync_MoreFrequentValue_ScoresHigherOnFrequency()
    {
        var db = TestDbContextFactory.Create();
        // "25" appears far more often than anything else
        var values = new List<string> { "25", "25", "25", "25", "25", "13", "47", "88", "02", "91" };
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values.ToArray(), new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.GetCandidatesAsync("1 PM", 2, null, null, 10);

        var top = result.Candidates.Single(c => c.Value == "25");
        Assert.True(top.Breakdown.FrequencyScore >= 99);
    }

    [Fact]
    public async Task GetCandidatesAsync_PopulatesHistoricalAndRecentFrequencyAndReason()
    {
        var db = TestDbContextFactory.Create();
        var values = new List<string> { "25", "25", "25", "13", "47" };
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values.ToArray(), new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.GetCandidatesAsync("1 PM", 2, null, null, 10);

        var top = result.Candidates.Single(c => c.Value == "25");
        Assert.Equal(3, top.HistoricalFrequency);
        Assert.True(top.RecentFrequency > 0);
        Assert.False(string.IsNullOrWhiteSpace(top.Reason));
        Assert.Equal("1 PM", result.DrawTime);
    }

    [Fact]
    public async Task GetCandidatesAsync_SupportsExactFiveDigitLength()
    {
        var db = TestDbContextFactory.Create();
        var values = Enumerable.Range(0, 20).Select(i => (10000 + i * 137).ToString("D5")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var result = await service.GetCandidatesAsync("1 PM", 5, null, null, 5);

        Assert.Equal(5, result.Candidates.Count);
        Assert.All(result.Candidates, c => Assert.Equal(5, c.Value.Length));
    }
}
