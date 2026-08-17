using LotteryAnalytics.Api.Services.Sambad;

namespace LotteryAnalytics.Api.Services.Results;

/// <summary>
/// Default provider when Sambad is not configured. Makes no external calls — results
/// are entered by an admin via /admin/results instead. Sync becomes a harmless no-op.
/// </summary>
public class ManualResultProvider : IResultProvider
{
    public Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default) =>
        Task.FromResult(new SambadFetchResult { Success = true, Results = new List<SambadResultDto>() });

    public string SourceName => "Manual";
}
