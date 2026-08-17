using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Notifications;
using LotteryAnalytics.Api.Services.Results;
using LotteryAnalytics.Api.Services.Sambad;

namespace LotteryAnalytics.Api.Tests;

public class FakeSambadApiClient : ISambadApiClient, IResultProvider
{
    private readonly SambadFetchResult _result;
    public FakeSambadApiClient(SambadFetchResult result) => _result = result;
    public Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default) =>
        Task.FromResult(_result);
    public string SourceName => "Sambad";
}

public class NoopNotificationService : INotificationService
{
    public Task SendResultNotificationAsync(LotteryResult result, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendAnalysisNotificationAsync(string drawTime, string summary, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendDailyReminderAsync(CancellationToken ct = default) => Task.CompletedTask;
}
