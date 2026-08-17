using System.Text.Json;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPush;

namespace LotteryAnalytics.Api.Services.Notifications;

/// <summary>Sends browser Web Push notifications (FCM-backed on Chrome/Android) via VAPID.</summary>
public class WebPushNotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(AppDbContext db, IOptions<WebPushOptions> options, ILogger<WebPushNotificationService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendResultNotificationAsync(LotteryResult result, CancellationToken ct = default)
    {
        var subs = await _db.NotificationSubscriptions
            .Where(s => s.ResultNotify && (s.DrawTimePreference == null || s.DrawTimePreference == result.DrawTime))
            .ToListAsync(ct);

        var payload = JsonSerializer.Serialize(new
        {
            title = $"{result.DrawTime} Result Published",
            body = $"Result: {result.ResultValue}",
            url = "/results"
        });

        await BroadcastAsync(subs, payload, ct);
    }

    public async Task SendAnalysisNotificationAsync(string drawTime, string summary, CancellationToken ct = default)
    {
        var subs = await _db.NotificationSubscriptions
            .Where(s => s.AnalysisNotify && (s.DrawTimePreference == null || s.DrawTimePreference == drawTime))
            .ToListAsync(ct);

        var payload = JsonSerializer.Serialize(new { title = $"{drawTime} Analysis Updated", body = summary, url = "/analysis" });
        await BroadcastAsync(subs, payload, ct);
    }

    public async Task SendDailyReminderAsync(CancellationToken ct = default)
    {
        var subs = await _db.NotificationSubscriptions.Where(s => s.DailyReminder).ToListAsync(ct);
        var payload = JsonSerializer.Serialize(new { title = "LotteryAnalytics", body = "Today's draws are open. Check the latest results.", url = "/" });
        await BroadcastAsync(subs, payload, ct);
    }

    private async Task BroadcastAsync(List<NotificationSubscription> subs, string payload, CancellationToken ct)
    {
        if (subs.Count == 0) return;

        if (string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            _logger.LogWarning("Web Push VAPID keys not configured; skipping {Count} notification(s).", subs.Count);
            return;
        }

        var client = new WebPushClient();
        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        var expired = new List<NotificationSubscription>();

        foreach (var sub in subs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, vapid, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
            {
                expired.Add(sub); // subscription no longer valid on the browser end
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push notification to subscription {Id}", sub.Id);
            }
        }

        if (expired.Count > 0)
        {
            _db.NotificationSubscriptions.RemoveRange(expired);
            await _db.SaveChangesAsync(ct);
        }
    }
}
