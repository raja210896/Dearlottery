using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;

namespace LotteryAnalytics.Api.Services.Predictions;

public class PredictionSaveOutcome
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public PredictionHistoryDto? Prediction { get; set; }

    public static PredictionSaveOutcome Fail(string error) => new() { Success = false, Error = error };
}

public interface IPredictionService
{
    Task<PredictionSaveOutcome> SaveSnapshotAsync(DateOnly drawDate, string drawTime, int digitLength, int count, CancellationToken ct = default);

    /// <summary>Evaluates any pending (unevaluated) predictions for the given result's draw date/time.</summary>
    Task EvaluatePendingAsync(LotteryResult result, CancellationToken ct = default);

    Task<PagedResult<PredictionHistoryDto>> GetHistoryAsync(
        DateOnly? from, DateOnly? to, string? drawTime, int? digitLength, string? matchStatus,
        int page, int pageSize, CancellationToken ct = default);

    Task<PredictionPerformanceDto> GetPerformanceAsync(string? drawTime, CancellationToken ct = default);
}
