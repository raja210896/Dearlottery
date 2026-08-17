using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Sambad;

/// <summary>Periodically triggers a results sync. Interval configurable via Sambad:SyncCronMinutes.</summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SambadOptions _options;
    private readonly ILogger<SyncBackgroundService> _logger;

    public SyncBackgroundService(IServiceScopeFactory scopeFactory, IOptions<SambadOptions> options, ILogger<SyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.SyncCronMinutes));
        var sambadConfigured = !string.IsNullOrWhiteSpace(_options.BaseUrl) && !string.IsNullOrWhiteSpace(_options.Token);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!sambadConfigured)
            {
                // Manual result mode: no external provider configured, nothing to sync on a schedule.
                await Task.Delay(interval, stoppingToken).ContinueWith(_ => { });
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                var outcome = await syncService.SyncTodayAsync("Scheduled", stoppingToken);
                if (!outcome.Success)
                {
                    _logger.LogWarning("Scheduled sync failed: {Message}", outcome.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled sync threw an unexpected error");
            }

            await Task.Delay(interval, stoppingToken).ContinueWith(_ => { }); // swallow cancellation
        }
    }
}
