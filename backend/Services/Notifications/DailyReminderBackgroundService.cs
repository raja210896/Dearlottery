using LotteryAnalytics.Api.Services.Notifications;

namespace LotteryAnalytics.Api.Services.Notifications;

/// <summary>Sends a daily reminder once per day to subscribers who opted in.</summary>
public class DailyReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyReminderBackgroundService> _logger;
    private DateOnly _lastSentDate = DateOnly.MinValue;

    public DailyReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DailyReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != _lastSentDate)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notifications.SendDailyReminderAsync(stoppingToken);
                    _lastSentDate = today;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Daily reminder send failed");
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ContinueWith(_ => { });
        }
    }
}
