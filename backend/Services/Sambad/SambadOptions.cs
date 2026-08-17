namespace LotteryAnalytics.Api.Services.Sambad;

public class SambadOptions
{
    public const string SectionName = "Sambad";
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    public int SyncCronMinutes { get; set; } = 30;
}
