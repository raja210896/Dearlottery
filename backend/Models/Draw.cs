namespace LotteryAnalytics.Api.Models;

public class Draw
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "1 PM", "6 PM", "8 PM"
    public TimeSpan ScheduledTime { get; set; }
    public bool IsActive { get; set; } = true;
}
