using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISyncService _syncService;

    public AdminController(AppDbContext db, ISyncService syncService)
    {
        _db = db;
        _syncService = syncService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var totalResults = await _db.LotteryResults.CountAsync(ct);
        var latestSync = await _db.SyncLogs.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(ct);
        var syncLogCount = await _db.SyncLogs.CountAsync(ct);

        return Ok(ApiResponse<DashboardSummary>.Ok(new DashboardSummary
        {
            TotalResults = totalResults,
            LatestSyncAt = latestSync?.StartedAt,
            LatestSyncSuccess = latestSync?.Success ?? false,
            LatestSyncMessage = latestSync?.Message,
            SyncLogCount = syncLogCount
        }));
    }

    [HttpGet("sync-logs")]
    public async Task<IActionResult> SyncLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.SyncLogs.AsNoTracking().OrderByDescending(s => s.StartedAt);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SyncLogDto
            {
                Id = s.Id,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                Success = s.Success,
                RecordsImported = s.RecordsImported,
                Message = s.Message,
                Trigger = s.Trigger
            }).ToListAsync(ct);

        return Ok(ApiResponse<PagedResult<SyncLogDto>>.Ok(new PagedResult<SyncLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> RunSync(CancellationToken ct)
    {
        var outcome = await _syncService.SyncTodayAsync("Manual", ct);
        return Ok(ApiResponse<object>.Ok(new { outcome.Success, outcome.Imported, outcome.Message }));
    }
}
