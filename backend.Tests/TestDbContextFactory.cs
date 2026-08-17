using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Tests;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Seeds `days` consecutive daily "1 PM" results with the given last-2-digit values (cycled).</summary>
    public static async Task SeedResultsAsync(AppDbContext db, string drawTime, string[] values, DateOnly startDate)
    {
        for (var i = 0; i < values.Length; i++)
        {
            db.LotteryResults.Add(new LotteryResult
            {
                DrawDate = startDate.AddDays(i),
                DrawTime = drawTime,
                ResultValue = values[i],
                Source = "Test"
            });
        }
        await db.SaveChangesAsync();
    }
}
