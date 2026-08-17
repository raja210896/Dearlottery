using LotteryAnalytics.Api.DTOs;

namespace LotteryAnalytics.Api.Services.Analysis;

public interface IBacktestService
{
    Task<BacktestResponse> RunAsync(
        string? drawTime, int digitLength, int drawCount, int candidateCount,
        DateOnly? from, DateOnly? to, CancellationToken ct = default);

    Task<DataQualitySummary> GetDataQualityAsync(string? drawTime, CancellationToken ct = default);

    /// <summary>Runs the same backtest at exact (5-digit), last-2, and last-3 granularities.</summary>
    Task<MultiDigitBacktestSummary> RunMultiAsync(
        string? drawTime, int drawCount, int candidateCount, DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
