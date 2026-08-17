namespace LotteryAnalytics.Api.Services.Notifications;

/// <summary>
/// Placeholder implementation. WhatsApp Business API integration is out of scope for v1
/// (requires a paid provider account and business verification) — intentionally not implemented.
/// </summary>
public class WhatsAppNotificationService : IWhatsAppNotificationService
{
    public Task SendMessageAsync(string toPhoneNumber, string message, CancellationToken ct = default) =>
        throw new NotSupportedException("WhatsApp notifications are not implemented in v1.");
}
