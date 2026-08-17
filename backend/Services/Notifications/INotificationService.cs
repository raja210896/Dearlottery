using LotteryAnalytics.Api.Models;

namespace LotteryAnalytics.Api.Services.Notifications;

public interface INotificationService
{
    Task SendResultNotificationAsync(LotteryResult result, CancellationToken ct = default);
    Task SendAnalysisNotificationAsync(string drawTime, string summary, CancellationToken ct = default);
    Task SendDailyReminderAsync(CancellationToken ct = default);
}
