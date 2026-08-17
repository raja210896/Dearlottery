using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Services.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/admin/import")]
[Authorize]
public class AdminImportController : ControllerBase
{
    private readonly IHistoricalImportService _importService;

    public AdminImportController(IHistoricalImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("csv")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(ApiResponse<object>.Fail("No file uploaded."));
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(ct);
        var summary = await _importService.ImportCsvAsync(content, ct);
        return Ok(ApiResponse<ImportSummary>.Ok(summary, "Import complete."));
    }

    [HttpPost("json")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportJson(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(ApiResponse<object>.Fail("No file uploaded."));
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(ct);

        ImportSummary summary;
        try
        {
            summary = await _importService.ImportJsonAsync(content, ct);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid JSON file."));
        }

        return Ok(ApiResponse<ImportSummary>.Ok(summary, "Import complete."));
    }
}
