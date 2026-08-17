using LotteryAnalytics.Api.DTOs;

namespace LotteryAnalytics.Api.Services.Analysis;

public interface IAnalysisService
{
    Task<FrequencySnapshot> GetFrequencyAsync(string? drawTime, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<List<RecencyEntry>> GetRecencyAsync(string? drawTime, int digitLength, int recentWindow, CancellationToken ct = default);
    Task<PatternStats> GetPatternStatsAsync(string? drawTime, int digitLength, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<AnalysisOverview> GetOverviewAsync(string? drawTime, CancellationToken ct = default);
    Task<DigitAnalysis> GetDigitAnalysisAsync(string? drawTime, DateOnly? from, DateOnly? to, int recentWindow, CancellationToken ct = default);
}
