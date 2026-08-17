using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private static readonly string[] DrawOrder = { "1 PM", "6 PM", "8 PM" };
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/results?page=1&pageSize=20&drawTime=&search=
    [HttpGet]
    public async Task<IActionResult> GetResults(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? drawTime = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.ResultValue.Contains(search));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.DrawDate).ThenBy(r => r.DrawTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResult<ResultDto>>.Ok(new PagedResult<ResultDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    // GET /api/results/today
    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = await _db.LotteryResults.AsNoTracking()
            .Where(r => r.DrawDate == today)
            .ToListAsync(ct);

        var byDraw = results.ToDictionary(r => r.DrawTime);
        var cards = DrawOrder.Select(draw => byDraw.TryGetValue(draw, out var r)
            ? ToDto(r)
            : new ResultDto { DrawDate = today, DrawTime = draw, Status = "Pending" }
        ).ToList();

        return Ok(ApiResponse<List<ResultDto>>.Ok(cards));
    }

    // GET /api/results/history?from=&to=&drawTime=&page=&pageSize=&sort=date_desc
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? drawTime,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "date_desc",
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);

        query = sort switch
        {
            "date_asc" => query.OrderBy(r => r.DrawDate).ThenBy(r => r.DrawTime),
            _ => query.OrderByDescending(r => r.DrawDate).ThenBy(r => r.DrawTime)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResult<ResultDto>>.Ok(new PagedResult<ResultDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    private static ResultDto ToDto(LotteryResult r) => new()
    {
        Id = r.Id,
        DrawDate = r.DrawDate,
        DrawTime = r.DrawTime,
        ResultValue = r.ResultValue,
        Status = "Published",
        LastUpdated = r.ImportedAt
    };
}
