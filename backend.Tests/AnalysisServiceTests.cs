using LotteryAnalytics.Api.Services.Analysis;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class AnalysisServiceTests
{
    [Fact]
    public async Task GetFrequencyAsync_CountsOccurrencesCorrectly()
    {
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM",
            new[] { "25", "25", "13", "07" }, new DateOnly(2026, 1, 1));

        var service = new AnalysisService(db);
        var freq = await service.GetFrequencyAsync("1 PM", null, null);

        Assert.Equal(4, freq.SampleSize);
        Assert.Equal(2, freq.Last2DigitFrequency.Single(e => e.Value == "25").Count);
        Assert.Equal(1, freq.Last2DigitFrequency.Single(e => e.Value == "13").Count);
        Assert.Contains(freq.HotNumbers, e => e.Value == "25");
    }

    [Fact]
    public async Task GetFrequencyAsync_NoResults_ReturnsEmptySnapshot()
    {
        var db = TestDbContextFactory.Create();
        var service = new AnalysisService(db);

        var freq = await service.GetFrequencyAsync("1 PM", null, null);

        Assert.Equal(0, freq.SampleSize);
        Assert.Empty(freq.FullNumberFrequency);
    }

    [Fact]
    public async Task GetDigitAnalysisAsync_ComputesDigitAndPositionFrequency()
    {
        var db = TestDbContextFactory.Create();
        // "11111" makes digit '1' appear 5x per draw, at every position, across 3 draws = 15 occurrences.
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM",
            new[] { "11111", "11111", "22222" }, new DateOnly(2026, 1, 1));

        var service = new AnalysisService(db);
        var analysis = await service.GetDigitAnalysisAsync("1 PM", null, null, 30);

        Assert.Equal(3, analysis.SampleSize);
        Assert.Equal(10, analysis.DigitFrequency.Single(e => e.Value == "1").Count); // 2 draws x 5 digits
        Assert.Equal(5, analysis.DigitFrequency.Single(e => e.Value == "2").Count);
        Assert.Contains(analysis.DigitFrequency.Take(3), e => e.Value == "1"); // hot digit
        Assert.Equal(5, analysis.PositionFrequency.Count); // 5-digit numbers -> 5 positions
        Assert.Equal(2, analysis.PositionFrequency[0].Digits.Single(d => d.Value == "1").Count);
    }

    [Fact]
    public async Task GetDigitAnalysisAsync_ComputesDigitPairFrequency()
    {
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM",
            new[] { "12345", "12999" }, new DateOnly(2026, 1, 1));

        var service = new AnalysisService(db);
        var analysis = await service.GetDigitAnalysisAsync("1 PM", null, null, 30);

        // "12" appears as the leading adjacent pair in both draws.
        Assert.Equal(2, analysis.DigitPairFrequency.Single(e => e.Value == "12").Count);
    }
}
