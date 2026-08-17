namespace LotteryAnalytics.Api.DTOs;

public class ScoreBreakdown
{
    public double FrequencyScore { get; set; }
    public double RecencyScore { get; set; }
    public double DigitScore { get; set; }
    public double RepeatScore { get; set; }
    public double PatternScore { get; set; }
}

public class Candidate
{
    public string Value { get; set; } = string.Empty;
    /// <summary>Statistical Model Score (0-100). NOT a probability of winning.</summary>
    public double ModelScore { get; set; }
    public ScoreBreakdown Breakdown { get; set; } = new();
    /// <summary>Raw occurrence count in the queried historical window.</summary>
    public int HistoricalFrequency { get; set; }
    /// <summary>Raw occurrence count within the most recent draws window.</summary>
    public int RecentFrequency { get; set; }
    /// <summary>Short, transparent explanation of the top contributing factors.</summary>
    public string Reason { get; set; } = string.Empty;
}

public class CandidateResponse
{
    public string DrawTime { get; set; } = "All draws";
    public List<Candidate> Candidates { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Statistical Candidates only. Model Score reflects historical pattern weighting, not a probability of winning. Past results do not guarantee future results.";
}
