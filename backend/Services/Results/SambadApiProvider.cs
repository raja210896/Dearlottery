using LotteryAnalytics.Api.Services.Sambad;

namespace LotteryAnalytics.Api.Services.Results;

/// <summary>Delegates to the existing Sambad API client. Selected only when Sambad is configured.</summary>
public class SambadApiProvider : IResultProvider
{
    private readonly ISambadApiClient _client;

    public SambadApiProvider(ISambadApiClient client)
    {
        _client = client;
    }

    public Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default) =>
        _client.FetchResultsAsync(date, ct);

    public string SourceName => "Sambad";
}
