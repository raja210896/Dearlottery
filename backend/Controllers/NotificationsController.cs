using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WebPushOptions _webPushOptions;

    public NotificationsController(AppDbContext db, IOptions<WebPushOptions> webPushOptions)
    {
        _db = db;
        _webPushOptions = webPushOptions.Value;
    }

    // Frontend needs the VAPID public key to create a PushSubscription.
    [HttpGet("public-key")]
    public IActionResult PublicKey() => Ok(ApiResponse<object>.Ok(new { publicKey = _webPushOptions.PublicKey }));

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return BadRequest(ApiResponse<object>.Fail("A push endpoint is required."));
        }

        var existing = await _db.NotificationSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);
        if (existing is null)
        {
            _db.NotificationSubscriptions.Add(new NotificationSubscription
            {
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                DrawTimePreference = request.DrawTimePreference,
                ResultNotify = request.ResultNotify,
                AnalysisNotify = request.AnalysisNotify,
                DailyReminder = request.DailyReminder
            });
        }
        else
        {
            existing.DrawTimePreference = request.DrawTimePreference;
            existing.ResultNotify = request.ResultNotify;
            existing.AnalysisNotify = request.AnalysisNotify;
            existing.DailyReminder = request.DailyReminder;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Subscribed."));
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request, CancellationToken ct)
    {
        var existing = await _db.NotificationSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);
        if (existing is not null)
        {
            _db.NotificationSubscriptions.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
        return Ok(ApiResponse<object>.Ok(new { }, "Unsubscribed."));
    }
}
