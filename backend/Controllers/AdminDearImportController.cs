using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Services.Dear;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/admin/dear-import")]
[Authorize]
public class AdminDearImportController : ControllerBase
{
    private readonly IDearBackfillService _backfill;

    public AdminDearImportController(IDearBackfillService backfill)
    {
        _backfill = backfill;
    }

    // POST /api/admin/dear-import/backfill?from=2025-05-01&to=2025-05-03
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (to < from)
        {
            return BadRequest(ApiResponse<object>.Fail("'to' must not be before 'from'."));
        }

        var summary = await _backfill.RunAsync(from, to, ct);
        return Ok(ApiResponse<DearBackfillSummary>.Ok(summary, "7Dear backfill complete."));
    }
}
