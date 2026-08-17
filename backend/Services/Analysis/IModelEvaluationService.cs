using LotteryAnalytics.Api.DTOs;

namespace LotteryAnalytics.Api.Services.Analysis;

public interface IModelEvaluationService
{
    /// <summary>
    /// Compares the existing Multi-Factor model against simple single-factor baselines
    /// (frequency-only, recency-only, random) and the current scoring model, per draw time,
    /// using identical chronological test windows with no future-data leakage. Read-only —
    /// does not touch the existing scoring formula or any LotteryResult data.
    /// </summary>
    Task<ModelComparisonResponse> CompareModelsAsync(
        int drawCount, int candidateCount, DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
