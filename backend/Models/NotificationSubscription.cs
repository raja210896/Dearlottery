namespace LotteryAnalytics.Api.Models;

public class NotificationSubscription
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? DrawTimePreference { get; set; } // null = all draws
    public bool ResultNotify { get; set; } = true;
    public bool AnalysisNotify { get; set; } = false;
    public bool DailyReminder { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
