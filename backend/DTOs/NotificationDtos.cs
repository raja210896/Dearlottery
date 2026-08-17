namespace LotteryAnalytics.Api.DTOs;

public class SubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? DrawTimePreference { get; set; }
    public bool ResultNotify { get; set; } = true;
    public bool AnalysisNotify { get; set; }
    public bool DailyReminder { get; set; }
}

public class UnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
