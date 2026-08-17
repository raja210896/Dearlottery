using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class SyncServiceDuplicateTests
{
    [Fact]
    public async Task SyncTodayAsync_RunTwice_DoesNotDuplicateResults()
    {
        var db = TestDbContextFactory.Create();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fetch = new SambadFetchResult
        {
            Success = true,
            Results = new List<SambadResultDto>
            {
                new() { DrawDate = today.ToString("yyyy-MM-dd"), DrawTime = "1 PM", Result = "42" }
            }
        };
        var client = new FakeSambadApiClient(fetch);
        var service = new SyncService(client, db, new NoopNotificationService(), NullLogger<SyncService>.Instance);

        var first = await service.SyncTodayAsync("Manual");
        var second = await service.SyncTodayAsync("Manual");

        Assert.Equal(1, first.Imported);
        Assert.Equal(0, second.Imported); // duplicate for the same draw date+time is skipped

        var storedCount = await db.LotteryResults.CountAsync(r => r.DrawDate == today && r.DrawTime == "1 PM");
        Assert.Equal(1, storedCount);
    }

    [Fact]
    public async Task SyncTodayAsync_ApiFailure_LogsFailureAndImportsNothing()
    {
        var db = TestDbContextFactory.Create();
        var client = new FakeSambadApiClient(new SambadFetchResult { Success = false, Error = "unreachable" });
        var service = new SyncService(client, db, new NoopNotificationService(), NullLogger<SyncService>.Instance);

        var outcome = await service.SyncTodayAsync("Manual");

        Assert.False(outcome.Success);
        Assert.Equal(0, await db.LotteryResults.CountAsync());
        Assert.Single(db.SyncLogs);
        Assert.False(db.SyncLogs.Single().Success);
    }
}
