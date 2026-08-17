namespace LotteryAnalytics.Api.Services.Results;

public interface IManualResultService
{
    Task<ManualResultOutcome> CreateAsync(DateOnly drawDate, string drawTime, string resultValue, CancellationToken ct = default);
    Task<ManualResultOutcome> UpdateAsync(int id, DateOnly drawDate, string drawTime, string resultValue, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
