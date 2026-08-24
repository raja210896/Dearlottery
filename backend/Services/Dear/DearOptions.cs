namespace LotteryAnalytics.Api.Services.Dear;

public class DearOptions
{
    public const string SectionName = "7Dear";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://www.7dear.in";
    /// <summary>Archive index source for <see cref="DearArchiveCollectorService"/> — a distinct
    /// site from <see cref="BaseUrl"/> (7dear.in); its own dated result pages are the source of
    /// truth for which date+draw links actually exist.</summary>
    public string ArchiveBaseUrl { get; set; } = "https://dearlottery.in";
    public DateOnly HistoricalStartDate { get; set; } = new(2025, 1, 1);
    public DateOnly HistoricalEndDate { get; set; } = new(2026, 8, 16);
    public bool DailyCheckEnabled { get; set; }
    public int TimeoutSeconds { get; set; } = 20;
    /// <summary>Delay between individual PDF requests, to avoid hammering the source site.</summary>
    public int RequestDelayMs { get; set; } = 500;
}
