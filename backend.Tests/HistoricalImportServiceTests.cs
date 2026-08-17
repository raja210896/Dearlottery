using LotteryAnalytics.Api.Services.Results;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LotteryAnalytics.Api.Tests;

public class HistoricalImportServiceTests
{
    [Fact]
    public async Task ImportCsvAsync_ValidRows_AreImported()
    {
        var db = TestDbContextFactory.Create();
        var service = new HistoricalImportService(db);
        var csv = "DrawDate,DrawTime,ResultValue\n2026-08-01,1 PM,42\n2026-08-01,6 PM,17\n2026-08-01,8 PM,83\n";

        var summary = await service.ImportCsvAsync(csv);

        Assert.Equal(3, summary.TotalRows);
        Assert.Equal(3, summary.Imported);
        Assert.Equal(0, summary.Invalid);
        Assert.Equal(0, summary.Duplicates);
        Assert.Equal(3, await db.LotteryResults.CountAsync());
        Assert.All(await db.LotteryResults.ToListAsync(), r => Assert.Equal("Import", r.Source));
    }

    [Fact]
    public async Task ImportJsonAsync_ValidRows_AreImported()
    {
        var db = TestDbContextFactory.Create();
        var service = new HistoricalImportService(db);
        var json = """[{"drawDate":"2026-08-02","drawTime":"1 PM","resultValue":"09"}]""";

        var summary = await service.ImportJsonAsync(json);

        Assert.Equal(1, summary.Imported);
        Assert.Equal(1, await db.LotteryResults.CountAsync());
    }

    [Fact]
    public async Task ImportCsvAsync_DuplicateAgainstExistingResult_IsSkipped()
    {
        var db = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedResultsAsync(db, "1 PM", new[] { "42" }, new DateOnly(2026, 8, 1));
        var service = new HistoricalImportService(db);
        var csv = "DrawDate,DrawTime,ResultValue\n2026-08-01,1 PM,99\n";

        var summary = await service.ImportCsvAsync(csv);

        Assert.Equal(0, summary.Imported);
        Assert.Equal(1, summary.Duplicates);
        Assert.Single(summary.Errors);
        Assert.Equal(1, await db.LotteryResults.CountAsync()); // original row untouched
    }

    [Fact]
    public async Task ImportCsvAsync_DuplicateWithinFile_IsSkipped()
    {
        var db = TestDbContextFactory.Create();
        var service = new HistoricalImportService(db);
        var csv = "DrawDate,DrawTime,ResultValue\n2026-08-01,1 PM,42\n2026-08-01,1 PM,99\n";

        var summary = await service.ImportCsvAsync(csv);

        Assert.Equal(1, summary.Imported);
        Assert.Equal(1, summary.Duplicates);
    }

    [Fact]
    public async Task ImportCsvAsync_InvalidRows_AreReportedNotFailed()
    {
        var db = TestDbContextFactory.Create();
        var service = new HistoricalImportService(db);
        var csv = "DrawDate,DrawTime,ResultValue\n" +
                   "not-a-date,1 PM,42\n" +      // invalid date
                   "2026-08-03,2 PM,42\n" +      // invalid draw time
                   "2026-08-03,1 PM,AB\n" +      // non-numeric result
                   "2026-08-04,1 PM,07\n";       // valid

        var summary = await service.ImportCsvAsync(csv);

        Assert.Equal(4, summary.TotalRows);
        Assert.Equal(1, summary.Imported);
        Assert.Equal(3, summary.Invalid);
        Assert.Equal(3, summary.Errors.Count);
        Assert.Equal(1, await db.LotteryResults.CountAsync());
    }
}
