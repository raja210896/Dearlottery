using LotteryAnalytics.Api.Models;

namespace LotteryAnalytics.Api.Services.Results;

public class ManualResultOutcome
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LotteryResult? Result { get; set; }
    /// <summary>True/false if a comparison against pre-draw statistical candidates was possible, else null.</summary>
    public bool? MatchedCandidate { get; set; }

    public static ManualResultOutcome Fail(string error) => new() { Success = false, Error = error };
}
