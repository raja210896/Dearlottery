namespace LotteryAnalytics.Api.Models;

public class LotteryResult
{
    public int Id { get; set; }
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty; // "1 PM" | "6 PM" | "8 PM"
    public string ResultValue { get; set; } = string.Empty; // numeric string e.g. "27"
    /// <summary>Lottery series code (e.g. "55C"), when the source provides one. Stored separately from ResultValue.</summary>
    public string? Series { get; set; }
    public string Source { get; set; } = "Sambad";
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
