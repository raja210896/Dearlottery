using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.AspNetCore.Mvc;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;

    public SyncController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("results")]
    public async Task<IActionResult> SyncResults(CancellationToken ct)
    {
        var outcome = await _syncService.SyncTodayAsync("Manual", ct);
        if (!outcome.Success)
        {
            return StatusCode(502, ApiResponse<object>.Fail(outcome.Message ?? "Sync failed."));
        }
        return Ok(ApiResponse<object>.Ok(new { imported = outcome.Imported }, "Sync completed."));
    }
}
