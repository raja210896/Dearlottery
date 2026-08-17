using LotteryAnalytics.Api.DTOs;

namespace LotteryAnalytics.Api.Services.Analysis;

public interface ICandidateScoringService
{
    Task<CandidateResponse> GetCandidatesAsync(
        string? drawTime, int digitLength, DateOnly? from, DateOnly? to, int count, CancellationToken ct = default);
}
