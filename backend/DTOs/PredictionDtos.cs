namespace LotteryAnalytics.Api.DTOs;

public class PredictionSaveRequest
{
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public int DigitLength { get; set; } = 2;
    public int Count { get; set; } = 10;
}

public class PredictionHistoryDto
{
    public int Id { get; set; }
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public int DigitLength { get; set; }
    public List<Candidate> Candidates { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public string? ActualResult { get; set; }
    public bool IsEvaluated { get; set; }
    public bool? MatchFound { get; set; }
    public int? MatchPosition { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public bool? ExactMatch { get; set; }
    public bool? Last3Match { get; set; }
    public bool? Last2Match { get; set; }
}

public class RecentPredictionOutcome
{
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public bool MatchFound { get; set; }
}

public class PredictionPerformanceDto
{
    public int TotalPredictions { get; set; }
    public int EvaluatedPredictions { get; set; }
    public int Matches { get; set; }
    /// <summary>Historical Match Rate (0-1) — NOT a probability of future winning.</summary>
    public double MatchRate { get; set; }
    public double RandomBaselineRate { get; set; }
    public List<RecentPredictionOutcome> RecentPerformance { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Historical comparison only. Model Score and Historical Match Rate are not a winning probability.";
}
