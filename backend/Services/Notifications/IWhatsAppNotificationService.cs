namespace LotteryAnalytics.Api.Services.Notifications;

/// <summary>
/// Abstraction reserved for a future WhatsApp Business API integration.
/// Not implemented in v1 — no WhatsApp infrastructure is wired up or faked.
/// </summary>
public interface IWhatsAppNotificationService
{
    Task SendMessageAsync(string toPhoneNumber, string message, CancellationToken ct = default);
}
