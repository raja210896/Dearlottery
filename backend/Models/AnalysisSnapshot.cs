namespace LotteryAnalytics.Api.Models;

public class AnalysisSnapshot
{
    public int Id { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public string SnapshotType { get; set; } = string.Empty; // "frequency" | "candidates" | "backtest"
    public string DataJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(6);
}
