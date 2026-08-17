namespace LotteryAnalytics.Api.Models;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public int RecordsImported { get; set; }
    public string? Message { get; set; }
    public string Trigger { get; set; } = "Manual"; // "Manual" | "Scheduled"
}
