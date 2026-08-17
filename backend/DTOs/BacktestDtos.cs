namespace LotteryAnalytics.Api.DTOs;

public class DrawTimeCount
{
    public string DrawTime { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DataQualitySummary
{
    public int TotalDraws { get; set; }
    public DateOnly? EarliestDate { get; set; }
    public DateOnly? LatestDate { get; set; }
    public List<DrawTimeCount> CountsByDrawTime { get; set; } = new();
    /// <summary>Expected daily slots (date × draw time) with no recorded result, within the earliest–latest range.</summary>
    public int MissingSlotCount { get; set; }
    public List<DateOnly> SampleMissingDates { get; set; } = new();
    /// <summary>Always 0 — DrawDate+DrawTime is DB-unique, so duplicates cannot be stored.</summary>
    public int DuplicateCount { get; set; }
}

public class BacktestDrawResult
{
    public DateOnly DrawDate { get; set; }
    public string ActualValue { get; set; } = string.Empty;
    public bool Hit { get; set; }
    public double TopScore { get; set; }
    public bool Top1 { get; set; }
    public bool Top5 { get; set; }
    public bool Top10 { get; set; }
}

public class BacktestResponse
{
    /// <summary>Draws in the requested window, before excluding any with insufficient prior history.</summary>
    public int TotalTested { get; set; }
    /// <summary>Draws actually evaluated (enough prior history existed). Same as historical "DrawsTested".</summary>
    public int DrawsTested { get; set; }
    public int Hits { get; set; }
    public double ModelHitRate { get; set; }
    public double RandomBaselineRate { get; set; }
    public double ModelVsRandomDifference { get; set; }

    public int Top1Matches { get; set; }
    public int Top5Matches { get; set; }
    public int Top10Matches { get; set; }
    public double Top1MatchRate { get; set; }
    public double Top5MatchRate { get; set; }
    public double Top10MatchRate { get; set; }

    public List<BacktestDrawResult> Draws { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Historical Match Rate only — not a winning probability or guarantee of future results. Lottery draws are random.";
}

/// <summary>Same backtest run at three digit granularities, for a combined report.</summary>
public class MultiDigitBacktestSummary
{
    public BacktestResponse Exact { get; set; } = new(); // full 5-digit number
    public BacktestResponse Last2 { get; set; } = new();
    public BacktestResponse Last3 { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Historical Match Rate only — not a winning probability or guarantee of future results. Lottery draws are random.";
}
