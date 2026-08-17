using LotteryAnalytics.Api.DTOs;

namespace LotteryAnalytics.Api.Services.Results;

public interface IHistoricalImportService
{
    Task<ImportSummary> ImportCsvAsync(string csvContent, CancellationToken ct = default);
    Task<ImportSummary> ImportJsonAsync(string jsonContent, CancellationToken ct = default);
}
