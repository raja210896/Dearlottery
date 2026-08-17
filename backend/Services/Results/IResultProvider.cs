using LotteryAnalytics.Api.Services.Sambad;

namespace LotteryAnalytics.Api.Services.Results;

/// <summary>
/// Abstracts where results come from for the sync pipeline. Default is manual entry
/// (no external calls); Sambad is opt-in once BaseUrl/Token are configured.
/// </summary>
public interface IResultProvider
{
    Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default);
    /// <summary>Short label stored on LotteryResult.Source for records this provider imports.</summary>
    string SourceName { get; }
}
