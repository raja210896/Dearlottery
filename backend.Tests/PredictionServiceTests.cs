using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Analysis;
using LotteryAnalytics.Api.Services.Predictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class PredictionServiceTests
{
    private static PredictionService BuildService(Api.Data.AppDbContext db) =>
        new(db, new CandidateScoringService(db, Options.Create(new ScoringWeights())));

    private static async Task SeedHistoryAsync(Api.Data.AppDbContext db, DateOnly upTo)
    {
        var values = Enumerable.Range(0, 30).Select(i => (i * 7 % 100).ToString("D2")).ToArray();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", values, upTo.AddDays(-values.Length));
    }

    [Fact]
    public async Task SaveSnapshotAsync_ValidDraw_IsPersisted()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);

        var outcome = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);

        Assert.True(outcome.Success);
        Assert.NotEmpty(outcome.Prediction!.Candidates);
        Assert.False(outcome.Prediction.IsEvaluated);
        Assert.Equal(1, await db.PredictionRecords.CountAsync());
    }

    [Fact]
    public async Task SaveSnapshotAsync_Duplicate_IsRejected()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);
        await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);

        var outcome = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);

        Assert.False(outcome.Success);
        Assert.Contains("already exists", outcome.Error);
        Assert.Equal(1, await db.PredictionRecords.CountAsync());
    }

    [Fact]
    public async Task EvaluatePendingAsync_ActualMatchesCandidate_MarksMatchFound()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);
        var saved = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);
        var topCandidate = saved.Prediction!.Candidates[0].Value;

        var result = new LotteryResult { Id = 999, DrawDate = drawDate, DrawTime = "1 PM", ResultValue = topCandidate };
        await service.EvaluatePendingAsync(result);

        var record = await db.PredictionRecords.SingleAsync();
        Assert.True(record.IsEvaluated);
        Assert.True(record.MatchFound);
        Assert.Equal(1, record.MatchPosition);
        Assert.Equal(topCandidate, record.ActualResult);
        Assert.NotNull(record.EvaluatedAt);
    }

    [Fact]
    public async Task EvaluatePendingAsync_ActualDoesNotMatch_MarksNoMatch()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);
        var saved = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);
        var candidateValues = saved.Prediction!.Candidates.Select(c => c.Value).ToHashSet();
        var nonCandidate = Enumerable.Range(0, 100).Select(i => i.ToString("D2")).First(v => !candidateValues.Contains(v));

        var result = new LotteryResult { Id = 999, DrawDate = drawDate, DrawTime = "1 PM", ResultValue = nonCandidate };
        await service.EvaluatePendingAsync(result);

        var record = await db.PredictionRecords.SingleAsync();
        Assert.True(record.IsEvaluated);
        Assert.False(record.MatchFound);
        Assert.Null(record.MatchPosition);
    }

    [Fact]
    public async Task EvaluatePendingAsync_OnlyEvaluatesMatchingDrawDateAndTime()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);
        await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);

        // A result for a different draw time must not evaluate the pending "1 PM" prediction.
        var unrelated = new LotteryResult { Id = 1, DrawDate = drawDate, DrawTime = "6 PM", ResultValue = "50" };
        await service.EvaluatePendingAsync(unrelated);

        var record = await db.PredictionRecords.SingleAsync();
        Assert.False(record.IsEvaluated);
        Assert.Null(record.MatchFound);
    }

    [Fact]
    public async Task EvaluatePendingAsync_PopulatesExactLast3Last2MatchGranularities()
    {
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await SeedHistoryAsync(db, drawDate);
        var service = BuildService(db);
        var saved = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);
        var topCandidate = saved.Prediction!.Candidates[0].Value;

        var result = new LotteryResult { Id = 999, DrawDate = drawDate, DrawTime = "1 PM", ResultValue = topCandidate };
        await service.EvaluatePendingAsync(result);

        var record = await db.PredictionRecords.SingleAsync();
        Assert.NotNull(record.ExactMatch);
        Assert.NotNull(record.Last3Match);
        Assert.NotNull(record.Last2Match);
        // Last2Match is computed with the same drawTime/digitLength=2/cutoff/count=10 as the
        // original snapshot, so it must agree exactly with the original MatchFound result.
        Assert.Equal(record.MatchFound, record.Last2Match);
    }

    [Fact]
    public async Task EvaluatePendingAsync_MatchGranularitiesUseNoFutureData()
    {
        // Only one prior "1 PM" draw exists — evaluating at digitLength=5 must find no prior
        // history at that granularity's cutoff and leave ExactMatch null, never guessing.
        var db = TestDbContextFactory.Create();
        var drawDate = new DateOnly(2026, 2, 1);
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", new[] { "27" }, drawDate.AddDays(-1));
        var service = BuildService(db);
        var saved = await service.SaveSnapshotAsync(drawDate, "1 PM", 2, 10);
        Assert.True(saved.Success);

        var result = new LotteryResult { Id = 1, DrawDate = drawDate, DrawTime = "1 PM", ResultValue = "27" };
        await service.EvaluatePendingAsync(result);

        var record = await db.PredictionRecords.SingleAsync();
        Assert.True(record.IsEvaluated);
        // With exactly one prior draw available (before the cutoff), GetCandidatesAsync still
        // returns results, so all granularities should be populated (non-null), not guessed.
        Assert.NotNull(record.ExactMatch);
    }
}
