using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Services.Predictions;
using Microsoft.AspNetCore.Mvc;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/analysis/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictions;

    public PredictionsController(IPredictionService predictions)
    {
        _predictions = predictions;
    }

    // POST /api/analysis/predictions
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PredictionSaveRequest request, CancellationToken ct)
    {
        var outcome = await _predictions.SaveSnapshotAsync(request.DrawDate, request.DrawTime, request.DigitLength, request.Count, ct);
        if (!outcome.Success) return BadRequest(ApiResponse<object>.Fail(outcome.Error!));
        return Ok(ApiResponse<PredictionHistoryDto>.Ok(outcome.Prediction!, "Prediction saved."));
    }

    // GET /api/analysis/predictions/history?from=&to=&drawTime=&digitLength=&matchStatus=&page=&pageSize=
    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? drawTime,
        [FromQuery] int? digitLength, [FromQuery] string? matchStatus,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _predictions.GetHistoryAsync(from, to, drawTime, digitLength, matchStatus, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<PredictionHistoryDto>>.Ok(result));
    }

    // GET /api/analysis/predictions/performance?drawTime=
    [HttpGet("performance")]
    public async Task<IActionResult> Performance([FromQuery] string? drawTime, CancellationToken ct)
    {
        var result = await _predictions.GetPerformanceAsync(drawTime, ct);
        return Ok(ApiResponse<PredictionPerformanceDto>.Ok(result));
    }
}
