namespace LotteryAnalytics.Api.Services.Sambad;

public interface ISambadApiClient
{
    /// <summary>Fetches published results for a date. Never throws — errors are returned in the result.</summary>
    Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default);
}
