namespace LotteryAnalytics.Api.Models;

public class PredictionRecord
{
    public int Id { get; set; }
    public DateOnly DrawDate { get; set; }
    public string DrawTime { get; set; } = string.Empty;
    public int DigitLength { get; set; }
    /// <summary>JSON-serialized List&lt;Candidate&gt; (value + Model Score), ordered by score descending.</summary>
    public string Candidates { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = string.Empty;

    public string? ActualResult { get; set; }
    public bool IsEvaluated { get; set; }
    public bool? MatchFound { get; set; }
    /// <summary>1-based rank of the matching candidate, or null if no match/not evaluated.</summary>
    public int? MatchPosition { get; set; }
    public DateTime? EvaluatedAt { get; set; }

    // Supplementary match flags at the three standard granularities, computed at evaluation time
    // against the same pre-draw cutoff as the original snapshot (no future-data leakage). Null until evaluated.
    public bool? ExactMatch { get; set; }
    public bool? Last3Match { get; set; }
    public bool? Last2Match { get; set; }

    public int? LotteryResultId { get; set; }
    public LotteryResult? LotteryResult { get; set; }
}
