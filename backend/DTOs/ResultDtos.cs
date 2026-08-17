namespace LotteryAnalytics.Api.DTOs;

public class ResultDto
{
    public int Id { get; set; }
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public string? ResultValue { get; set; } // null = not yet published
    public string Status { get; set; } = "Pending"; // "Pending" | "Published"
    public DateTime? LastUpdated { get; set; }
}
