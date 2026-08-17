using System.Text.Json;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Services.Results;

/// <summary>
/// Imports permitted/user-owned historical CSV/JSON data into the existing LotteryResult
/// table. Reuses the DrawDate+DrawTime unique constraint for duplicate prevention; one bad
/// row never fails the whole import.
/// </summary>
public class HistoricalImportService : IHistoricalImportService
{
    private static readonly HashSet<string> ValidDrawTimes = new() { "1 PM", "6 PM", "8 PM" };
    private const int MaxReportedErrors = 50;

    private readonly AppDbContext _db;

    public HistoricalImportService(AppDbContext db)
    {
        _db = db;
    }

    public Task<ImportSummary> ImportCsvAsync(string csvContent, CancellationToken ct = default) =>
        RunImportAsync(ParseCsv(csvContent), ct);

    public Task<ImportSummary> ImportJsonAsync(string jsonContent, CancellationToken ct = default) =>
        RunImportAsync(ParseJson(jsonContent), ct);

    private async Task<ImportSummary> RunImportAsync(List<RawRow> rows, CancellationToken ct)
    {
        var summary = new ImportSummary { TotalRows = rows.Count };
        var valid = new List<(RawRow Row, DateOnly Date)>();
        var seenInFile = new HashSet<(DateOnly, string)>();

        foreach (var row in rows)
        {
            var error = Validate(row, out var date);
            if (error is not null)
            {
                summary.Invalid++;
                AddError(summary, row.RowNumber, error);
                continue;
            }

            if (!seenInFile.Add((date, row.DrawTime)))
            {
                summary.Duplicates++;
                AddError(summary, row.RowNumber, "Duplicate of another row in this file.");
                continue;
            }

            valid.Add((row, date));
        }

        if (valid.Count == 0)
        {
            summary.Skipped = summary.Invalid + summary.Duplicates;
            return summary;
        }

        // One batch query for existing keys in the imported date range, instead of one query per row.
        var minDate = valid.Min(v => v.Date);
        var maxDate = valid.Max(v => v.Date);
        var existing = await _db.LotteryResults.AsNoTracking()
            .Where(r => r.DrawDate >= minDate && r.DrawDate <= maxDate)
            .Select(r => new { r.DrawDate, r.DrawTime })
            .ToListAsync(ct);
        var existingSet = existing.Select(k => (k.DrawDate, k.DrawTime)).ToHashSet();

        var toInsert = new List<LotteryResult>();
        var affectedDrawTimes = new HashSet<string>();
        foreach (var (row, date) in valid)
        {
            if (existingSet.Contains((date, row.DrawTime)))
            {
                summary.Duplicates++;
                AddError(summary, row.RowNumber, "A result for this draw date and time already exists.");
                continue;
            }

            toInsert.Add(new LotteryResult
            {
                DrawDate = date,
                DrawTime = row.DrawTime,
                ResultValue = row.ResultValue,
                Source = "Import",
                ImportedAt = DateTime.UtcNow
            });
            affectedDrawTimes.Add(row.DrawTime);
        }

        if (toInsert.Count > 0)
        {
            _db.LotteryResults.AddRange(toInsert); // single batch insert
            await _db.SaveChangesAsync(ct);
            summary.Imported = toInsert.Count;

            var stale = await _db.AnalysisSnapshots
                .Where(s => affectedDrawTimes.Contains(s.DrawTime) || s.DrawTime == "all")
                .ToListAsync(ct);
            if (stale.Count > 0)
            {
                _db.AnalysisSnapshots.RemoveRange(stale);
                await _db.SaveChangesAsync(ct);
            }
        }

        summary.Skipped = summary.Invalid + summary.Duplicates;
        return summary;
    }

    private static string? Validate(RawRow row, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(row.DrawDate) || string.IsNullOrWhiteSpace(row.DrawTime) || string.IsNullOrWhiteSpace(row.ResultValue))
            return "Missing required field (DrawDate, DrawTime, or ResultValue).";
        if (!DateOnly.TryParse(row.DrawDate, out date))
            return $"Invalid date format: '{row.DrawDate}'.";
        if (!ValidDrawTimes.Contains(row.DrawTime.Trim()))
            return $"Unsupported draw time: '{row.DrawTime}'.";
        if (!row.ResultValue.All(char.IsDigit) || row.ResultValue.Length > 10)
            return $"Invalid result value: '{row.ResultValue}'.";
        return null;
    }

    private static void AddError(ImportSummary summary, int rowNumber, string reason)
    {
        if (summary.Errors.Count < MaxReportedErrors)
            summary.Errors.Add(new ImportRowError { RowNumber = rowNumber, Reason = reason });
    }

    private static List<RawRow> ParseCsv(string content)
    {
        var rows = new List<RawRow>();
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
        if (lines.Count == 0) return rows;

        var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToList();
        var dateIdx = header.IndexOf("drawdate");
        var timeIdx = header.IndexOf("drawtime");
        var valueIdx = header.IndexOf("resultvalue");
        if (dateIdx < 0 || timeIdx < 0 || valueIdx < 0) return rows; // no valid header — nothing to import

        for (var i = 1; i < lines.Count; i++)
        {
            var cols = lines[i].Split(',');
            var get = (int idx) => idx < cols.Length ? cols[idx].Trim().Trim('"') : string.Empty;
            rows.Add(new RawRow(i + 1, get(dateIdx), get(timeIdx), get(valueIdx)));
        }
        return rows;
    }

    private static List<RawRow> ParseJson(string content)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = JsonSerializer.Deserialize<List<ImportRowDto>>(content, options) ?? new();
        return items.Select((item, i) => new RawRow(i + 2, item.DrawDate, item.DrawTime, item.ResultValue)).ToList();
    }

    private record RawRow(int RowNumber, string DrawDate, string DrawTime, string ResultValue);
}
