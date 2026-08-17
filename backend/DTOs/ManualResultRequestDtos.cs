namespace LotteryAnalytics.Api.DTOs;

public class ManualResultRequest
{
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
}
