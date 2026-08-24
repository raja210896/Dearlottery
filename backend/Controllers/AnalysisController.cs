using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Services.Analysis;
using Microsoft.AspNetCore.Mvc;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysis;
    private readonly ICandidateScoringService _scoring;
    private readonly IBacktestService _backtest;
    private readonly IModelEvaluationService _modelEvaluation;

    public AnalysisController(IAnalysisService analysis, ICandidateScoringService scoring, IBacktestService backtest, IModelEvaluationService modelEvaluation)
    {
        _analysis = analysis;
        _scoring = scoring;
        _backtest = backtest;
        _modelEvaluation = modelEvaluation;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] string? drawTime, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetOverviewAsync(drawTime, ct)));

    [HttpGet("frequency")]
    public async Task<IActionResult> Frequency([FromQuery] string? drawTime, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetFrequencyAsync(drawTime, from, to, ct)));

    [HttpGet("recency")]
    public async Task<IActionResult> Recency([FromQuery] string? drawTime, [FromQuery] int digitLength = 2, [FromQuery] int recentWindow = 30, CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetRecencyAsync(drawTime, digitLength, recentWindow, ct)));

    [HttpGet("patterns")]
    public async Task<IActionResult> Patterns([FromQuery] string? drawTime, [FromQuery] int digitLength = 2, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetPatternStatsAsync(drawTime, digitLength, from, to, ct)));

    // GET /api/analysis/candidates?draw=&digitLength=2&from=&to=&count=10
    [HttpGet("candidates")]
    public async Task<IActionResult> Candidates(
        [FromQuery] string? draw,
        [FromQuery] int digitLength = 2,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int count = 10,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _scoring.GetCandidatesAsync(draw, digitLength, from, to, count, ct)));

    // GET /api/analysis/backtest?draw=&digitLength=2&drawCount=30&candidateCount=10
    [HttpGet("backtest")]
    public async Task<IActionResult> Backtest(
        [FromQuery] string? draw,
        [FromQuery] int digitLength = 2,
        [FromQuery] int drawCount = 30,
        [FromQuery] int candidateCount = 10,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _backtest.RunAsync(draw, digitLength, drawCount, candidateCount, from, to, ct)));

    // GET /api/analysis/backtest/data-quality?draw=
    [HttpGet("backtest/data-quality")]
    public async Task<IActionResult> DataQuality([FromQuery] string? draw, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _backtest.GetDataQualityAsync(draw, ct)));

    // GET /api/analysis/backtest/multi?draw=&drawCount=30&candidateCount=10
    [HttpGet("backtest/multi")]
    public async Task<IActionResult> BacktestMulti(
        [FromQuery] string? draw,
        [FromQuery] int drawCount = 30,
        [FromQuery] int candidateCount = 10,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _backtest.RunMultiAsync(draw, drawCount, candidateCount, from, to, ct)));

    // GET /api/analysis/digits?drawTime=&from=&to=&recentWindow=30
    [HttpGet("digits")]
    public async Task<IActionResult> Digits(
        [FromQuery] string? drawTime,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int recentWindow = 30,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetDigitAnalysisAsync(drawTime, from, to, recentWindow, ct)));

    // GET /api/analysis/seasonal?date=&digitLength=2&topN=6
    // Read-only: same-date-last-year + current-month frequency. Does not use or affect the
    // Multi-Factor candidate scoring model or saved PredictionRecords.
    [HttpGet("seasonal")]
    public async Task<IActionResult> Seasonal(
        [FromQuery] DateOnly? date,
        [FromQuery] int digitLength = 2,
        [FromQuery] int topN = 6,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _analysis.GetSeasonalPatternAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), digitLength, topN, ct)));

    // GET /api/analysis/model-comparison?drawCount=20&candidateCount=10&from=&to=
    [HttpGet("model-comparison")]
    public async Task<IActionResult> ModelComparison(
        [FromQuery] int drawCount = 20,
        [FromQuery] int candidateCount = 10,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default) =>
        Ok(ApiResponse<object>.Ok(await _modelEvaluation.CompareModelsAsync(drawCount, candidateCount, from, to, ct)));
}
