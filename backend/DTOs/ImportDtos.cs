namespace LotteryAnalytics.Api.DTOs;

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ImportSummary
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Duplicates { get; set; }
    public int Invalid { get; set; }
    public List<ImportRowError> Errors { get; set; } = new();
}

/// <summary>Row shape for JSON import — property names are matched case-insensitively.</summary>
public class ImportRowDto
{
    public string DrawDate { get; set; } = string.Empty;
    public string DrawTime { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
}
