using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Services.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/admin/results")]
[Authorize]
public class AdminResultsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IManualResultService _manualResults;

    public AdminResultsController(AppDbContext db, IManualResultService manualResults)
    {
        _db = db;
        _manualResults = manualResults;
    }

    // GET /api/admin/results?page=&pageSize=&drawTime=&search=&from=&to=
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? drawTime = null, [FromQuery] string? search = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.LotteryResults.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(drawTime)) query = query.Where(r => r.DrawTime == drawTime);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.ResultValue.Contains(search));
        if (from.HasValue) query = query.Where(r => r.DrawDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DrawDate <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.DrawDate).ThenBy(r => r.DrawTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ResultDto
            {
                Id = r.Id, DrawDate = r.DrawDate, DrawTime = r.DrawTime, ResultValue = r.ResultValue,
                Status = "Published", LastUpdated = r.ImportedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResult<ResultDto>>.Ok(new PagedResult<ResultDto>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ManualResultRequest request, CancellationToken ct)
    {
        var outcome = await _manualResults.CreateAsync(request.DrawDate, request.DrawTime, request.ResultValue, ct);
        if (!outcome.Success) return BadRequest(ApiResponse<object>.Fail(outcome.Error!));
        return Ok(ApiResponse<object>.Ok(new { result = outcome.Result, matchedCandidate = outcome.MatchedCandidate }, "Result saved."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ManualResultRequest request, CancellationToken ct)
    {
        var outcome = await _manualResults.UpdateAsync(id, request.DrawDate, request.DrawTime, request.ResultValue, ct);
        if (!outcome.Success) return BadRequest(ApiResponse<object>.Fail(outcome.Error!));
        return Ok(ApiResponse<object>.Ok(new { result = outcome.Result }, "Result updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _manualResults.DeleteAsync(id, ct);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Result not found."));
        return Ok(ApiResponse<object>.Ok(new { }, "Result deleted."));
    }
}
