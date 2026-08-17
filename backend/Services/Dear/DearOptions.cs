namespace LotteryAnalytics.Api.Services.Dear;

public class DearOptions
{
    public const string SectionName = "7Dear";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://www.7dear.in";
    public DateOnly HistoricalStartDate { get; set; } = new(2025, 1, 1);
    public DateOnly HistoricalEndDate { get; set; } = new(2026, 8, 16);
    public bool DailyCheckEnabled { get; set; }
    public int TimeoutSeconds { get; set; } = 20;
    /// <summary>Delay between individual PDF requests, to avoid hammering the source site.</summary>
    public int RequestDelayMs { get; set; } = 500;
}
