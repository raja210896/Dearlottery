namespace LotteryAnalytics.Api.Services.Sambad;

/// <summary>Raw shape returned by the Sambad results endpoint (per current published docs).</summary>
public class SambadResultDto
{
    public string DrawDate { get; set; } = string.Empty; // yyyy-MM-dd
    public string DrawTime { get; set; } = string.Empty; // "1 PM" | "6 PM" | "8 PM"
    public string Result { get; set; } = string.Empty;
    /// <summary>Optional series code, when the source provides one (e.g. 7Dear).</summary>
    public string? Series { get; set; }
}

public class SambadFetchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<SambadResultDto> Results { get; set; } = new();
}
