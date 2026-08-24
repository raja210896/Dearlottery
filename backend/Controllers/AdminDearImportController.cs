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
    private readonly DearArchiveCollectorService _archive;

    public AdminDearImportController(IDearBackfillService backfill, DearArchiveCollectorService archive)
    {
        _backfill = backfill;
        _archive = archive;
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

    // POST /api/admin/dear-import/archive-sync
    // Reconciles LotteryResults against https://dearlottery.in/old-lottery-sambad — inserts only
    // date+draw combinations the archive actually links to and that aren't already in the database.
    [HttpPost("archive-sync")]
    public async Task<IActionResult> ArchiveSync(CancellationToken ct)
    {
        var summary = await _archive.RunAsync(ct);
        return Ok(ApiResponse<DearArchiveSyncSummary>.Ok(summary, "Dear Lottery archive sync complete."));
    }
}
