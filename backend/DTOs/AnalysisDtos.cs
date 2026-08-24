namespace LotteryAnalytics.Api.DTOs;

public class FrequencyEntry
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FrequencySnapshot
{
    public List<FrequencyEntry> FullNumberFrequency { get; set; } = new();
    public List<FrequencyEntry> LastDigitFrequency { get; set; } = new();
    public List<FrequencyEntry> Last2DigitFrequency { get; set; } = new();
    public List<FrequencyEntry> Last3DigitFrequency { get; set; } = new();
    public List<FrequencyEntry> HotNumbers { get; set; } = new(); // top 10 most frequent (last 2 digit)
    public List<FrequencyEntry> ColdNumbers { get; set; } = new(); // bottom 10 least frequent (last 2 digit)
    public int SampleSize { get; set; }
}

public class RecencyEntry
{
    public string Value { get; set; } = string.Empty;
    public DateOnly? LastAppearance { get; set; }
    public int DrawsSinceAppearance { get; set; }
    public int RecentFrequency { get; set; } // occurrences in last N draws
}

public class RecentRepeat
{
    public string Value { get; set; } = string.Empty;
    public DateOnly FirstDate { get; set; }
    public DateOnly SecondDate { get; set; }
    public int DrawsApart { get; set; }
}

public class PatternStats
{
    public int OddCount { get; set; }
    public int EvenCount { get; set; }
    public Dictionary<int, int> DigitSumDistribution { get; set; } = new();
    public int RepeatedDigitCount { get; set; } // results where digits repeat within the number (e.g. "44")
    public List<RecentRepeat> RecentRepeats { get; set; } = new();
}

public class AnalysisOverview
{
    public FrequencySnapshot Frequency { get; set; } = new();
    public List<RecencyEntry> Recency { get; set; } = new();
    public PatternStats Patterns { get; set; } = new();
}

public class PositionFrequency
{
    /// <summary>1-based digit position within the number (1st, 2nd, ...).</summary>
    public int Position { get; set; }
    public List<FrequencyEntry> Digits { get; set; } = new(); // "0".."9" -> count at this position
}

public class RecentVsHistoricalEntry
{
    public string Value { get; set; } = string.Empty;
    public int HistoricalCount { get; set; }
    public int RecentCount { get; set; }
}

public class SeasonalDrawPrediction
{
    public string DrawTime { get; set; } = string.Empty;
    /// <summary>The exact date being compared against (target date minus 1 year).</summary>
    public DateOnly SameDateLastYear { get; set; }
    /// <summary>Null when no result exists for that date+draw — never guessed/fabricated.</summary>
    public string? SameDateLastYearValue { get; set; }
    /// <summary>Numbers ranked by how often they occurred in this calendar month, across all years on record.</summary>
    public List<FrequencyEntry> CurrentMonthFrequency { get; set; } = new();
    public int CurrentMonthSampleSize { get; set; }
}

public class SeasonalPattern
{
    public DateOnly TargetDate { get; set; }
    public List<SeasonalDrawPrediction> Draws { get; set; } = new();
    public string Disclaimer { get; set; } =
        "Statistical pattern only — same-date-last-year and current-month frequency, not a prediction of future outcomes.";
}

public class DigitAnalysis
{
    /// <summary>Frequency of each digit 0-9 across all positions of all numbers.</summary>
    public List<FrequencyEntry> DigitFrequency { get; set; } = new();
    public List<FrequencyEntry> HotDigits { get; set; } = new(); // top 3 of DigitFrequency
    public List<FrequencyEntry> ColdDigits { get; set; } = new(); // bottom 3 of DigitFrequency
    public List<PositionFrequency> PositionFrequency { get; set; } = new();
    /// <summary>Frequency of adjacent digit pairs within each number (also represents digit transition patterns).</summary>
    public List<FrequencyEntry> DigitPairFrequency { get; set; } = new();
    /// <summary>Last-2-digit values compared: recent window vs full history.</summary>
    public List<RecentVsHistoricalEntry> RecentVsHistorical { get; set; } = new();
    public int SampleSize { get; set; }
}
