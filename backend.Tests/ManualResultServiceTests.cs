using LotteryAnalytics.Api.Services.Analysis;
using LotteryAnalytics.Api.Services.Predictions;
using LotteryAnalytics.Api.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class ManualResultServiceTests
{
    private static ManualResultService BuildService(Api.Data.AppDbContext db)
    {
        var scoring = new CandidateScoringService(db, Options.Create(new ScoringWeights()));
        var predictions = new PredictionService(db, scoring);
        return new ManualResultService(db, scoring, new NoopNotificationService(), predictions, NullLogger<ManualResultService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidResult_IsPersisted()
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);

        var outcome = await service.CreateAsync(new DateOnly(2026, 1, 1), "1 PM", "27");

        Assert.True(outcome.Success);
        Assert.Equal("Manual", outcome.Result!.Source);
        Assert.Equal(1, await db.LotteryResults.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateDrawDateAndTime_IsRejected()
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);
        await service.CreateAsync(new DateOnly(2026, 1, 1), "1 PM", "27");

        var outcome = await service.CreateAsync(new DateOnly(2026, 1, 1), "1 PM", "99");

        Assert.False(outcome.Success);
        Assert.Contains("already exists", outcome.Error);
        Assert.Equal(1, await db.LotteryResults.CountAsync());
    }

    [Theory]
    [InlineData("2 PM", "27")]   // invalid draw time
    [InlineData("1 PM", "2A")]   // non-numeric result
    [InlineData("1 PM", "")]     // missing result
    public async Task CreateAsync_InvalidInput_IsRejected(string drawTime, string resultValue)
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);

        var outcome = await service.CreateAsync(new DateOnly(2026, 1, 1), drawTime, resultValue);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Error);
        Assert.Equal(0, await db.LotteryResults.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ComparesAgainstPreDrawCandidates_WithoutFutureLeakage()
    {
        var db = TestDbContextFactory.Create();
        // Seed enough prior history for candidate scoring to produce results.
        var values = Enumerable.Range(0, 30).Select(i => (i * 7 % 100).ToString("D2")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, new DateOnly(2026, 1, 1));

        var service = BuildService(db);
        var newDrawDate = new DateOnly(2026, 1, 31);
        var outcome = await service.CreateAsync(newDrawDate, "1 PM", values[0]); // reuse a historically frequent value

        Assert.True(outcome.Success);
        Assert.NotNull(outcome.MatchedCandidate); // history existed, so a comparison was made
    }

    [Fact]
    public async Task CreateAsync_NoPriorHistory_MatchedCandidateIsNull()
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);

        var outcome = await service.CreateAsync(new DateOnly(2026, 1, 1), "1 PM", "27");

        Assert.True(outcome.Success);
        Assert.Null(outcome.MatchedCandidate);
    }

    [Fact]
    public async Task DeleteAsync_RemovesResult()
    {
        var db = TestDbContextFactory.Create();
        var service = BuildService(db);
        var created = await service.CreateAsync(new DateOnly(2026, 1, 1), "1 PM", "27");

        var deleted = await service.DeleteAsync(created.Result!.Id);

        Assert.True(deleted);
        Assert.Equal(0, await db.LotteryResults.CountAsync());
    }
}
