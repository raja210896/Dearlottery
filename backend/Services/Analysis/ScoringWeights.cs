namespace LotteryAnalytics.Api.Services.Analysis;

/// <summary>Configurable weights for the candidate scoring model. Must sum to 1.0 (normalized if not).</summary>
public class ScoringWeights
{
    public const string SectionName = "ScoringWeights";
    public double Frequency { get; set; } = 0.30;
    public double Recency { get; set; } = 0.25;
    public double Digit { get; set; } = 0.15;
    public double Repeat { get; set; } = 0.15;
    public double Pattern { get; set; } = 0.15;
}
