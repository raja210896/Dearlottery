namespace LotteryAnalytics.Api.DTOs;

public class ModelHitRateResult
{
    public int DrawsTested { get; set; }
    /// <summary>-1 for the Random baseline, which is computed analytically (candidateCount / universe), not simulated.</summary>
    public int Hits { get; set; }
    public double HitRate { get; set; }
}

public class ModelDigitResults
{
    public ModelHitRateResult Exact { get; set; } = new();
    public ModelHitRateResult Last3 { get; set; } = new();
    public ModelHitRateResult Last2 { get; set; } = new();
}

public class DrawTimeModelComparison
{
    public string DrawTime { get; set; } = string.Empty;
    public ModelDigitResults MultiFactor { get; set; } = new();
    public ModelDigitResults FrequencyOnly { get; set; } = new();
    public ModelDigitResults RecencyOnly { get; set; } = new();
    public ModelDigitResults Random { get; set; } = new();
}

public class ModelComparisonResponse
{
    public List<DrawTimeModelComparison> ByDrawTime { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Historical comparison only, using identical chronological test windows with no future-data leakage. " +
        "Does not indicate statistical predictability or guaranteed accuracy of future lottery draws.";
}
