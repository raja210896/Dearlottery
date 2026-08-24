using System.Text.RegularExpressions;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Services.Results;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Dear;

public class DearArchiveSyncSummary
{
    /// <summary>Distinct dates the archive index actually lists at least one available draw for.</summary>
    public int DatesListedByArchive { get; set; }
    public int PagesFetched { get; set; }
    public int Inserted { get; set; }
    public int SkippedExisting { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Reconciles LotteryResults against the archive index at https://dearlottery.in/old-lottery-sambad
/// (a different site from the existing PDF-based <see cref="DearLotteryCollectorService"/>, which
/// stays untouched). The archive page itself is the source of truth for which DrawDate+DrawTime
/// links exist — a date/draw is never fetched or inserted unless the archive actually links to it;
/// "—" (No Draw) cells have no link at all and are simply never visited.
/// </summary>
public class DearArchiveCollectorService
{
    private static readonly Dictionary<string, string> SlotToDrawTime = new()
    {
        ["1pm"] = "1 PM",
        ["6pm"] = "6 PM",
        ["8pm"] = "8 PM",
    };

    private static readonly Regex AvailableLinkPattern =
        new(@"href=""/lottery-result-(\d{2})-(\d{2})-(\d{2})#(1pm|6pm|8pm)""", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ISyncService _syncService;
    private readonly DearOptions _options;
    private readonly ILogger<DearArchiveCollectorService> _logger;

    public DearArchiveCollectorService(HttpClient http, AppDbContext db, ISyncService syncService, IOptions<DearOptions> options, ILogger<DearArchiveCollectorService> logger)
    {
        _http = http;
        _db = db;
        _syncService = syncService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DearArchiveSyncSummary> RunAsync(CancellationToken ct = default)
    {
        var summary = new DearArchiveSyncSummary();
        _logger.LogInformation("[DearArchive] Sync started");

        var indexHtml = await FetchIndexHtmlAsync(ct);
        if (indexHtml is null)
        {
            summary.Errors.Add("Could not fetch the archive index page.");
            return summary;
        }

        // Every (date, drawTime) the archive actually links to — never a guessed/constructed URL.
        var availableByDate = new Dictionary<DateOnly, List<string>>();
        foreach (Match m in AvailableLinkPattern.Matches(indexHtml))
        {
            if (!TryParseArchiveDate(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, out var date)) continue;
            var drawTime = SlotToDrawTime[m.Groups[4].Value];

            if (!availableByDate.TryGetValue(date, out var list))
            {
                list = new List<string>();
                availableByDate[date] = list;
            }
            if (!list.Contains(drawTime)) list.Add(drawTime);
        }
        summary.DatesListedByArchive = availableByDate.Count;

        // Drop draws already in the database so we never re-fetch or re-insert them.
        var existingSet = (await _db.LotteryResults.Select(r => new { r.DrawDate, r.DrawTime }).ToListAsync(ct))
            .Select(e => (e.DrawDate, e.DrawTime)).ToHashSet();
        foreach (var date in availableByDate.Keys.ToList())
        {
            availableByDate[date] = availableByDate[date].Where(dt => !existingSet.Contains((date, dt))).ToList();
            if (availableByDate[date].Count == 0) availableByDate.Remove(date);
        }

        foreach (var (date, neededDrawTimes) in availableByDate)
        {
            ct.ThrowIfCancellationRequested();

            var results = await FetchAndParseResultPageAsync(date, neededDrawTimes, ct);
            summary.PagesFetched++;

            if (results.Count > 0)
            {
                var provider = new PreloadedResultProvider(results, "DearLotteryArchive");
                var outcome = await _syncService.SyncDateWithProviderAsync(provider, date, "Archive-DearLottery", ct);
                if (outcome.Success)
                {
                    summary.Inserted += outcome.Imported;
                    summary.SkippedExisting += outcome.SkippedExisting;
                }
                else
                {
                    summary.Errors.Add($"{date:yyyy-MM-dd}: {outcome.Message}");
                }
            }

            if (_options.RequestDelayMs > 0) await Task.Delay(_options.RequestDelayMs, ct);
        }

        _logger.LogInformation(
            "[DearArchive] Sync completed: Inserted={Inserted}, Skipped={Skipped}, Listed={Listed}",
            summary.Inserted, summary.SkippedExisting, summary.DatesListedByArchive);
        return summary;
    }

    private async Task<string?> FetchIndexHtmlAsync(CancellationToken ct)
    {
        try
        {
            var main = await _http.GetStringAsync("/old-lottery-sambad", ct);
            var extras = string.Empty;
            try
            {
                extras = await _http.GetStringAsync("/old-lottery-sambad/extras", ct);
            }
            catch (Exception)
            {
                // The "Show More" fragment is optional — its absence just means no older months.
            }
            return main + extras;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DearArchive] Failed to fetch archive index");
            return null;
        }
    }

    private async Task<List<SambadResultDto>> FetchAndParseResultPageAsync(DateOnly date, List<string> neededDrawTimes, CancellationToken ct)
    {
        var results = new List<SambadResultDto>();
        string html;
        var path = $"/lottery-result-{date:dd-MM-yy}";

        try
        {
            using var response = await _http.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[DearArchive] Result page unavailable for {Date} ({StatusCode})", date, response.StatusCode);
                return results;
            }
            html = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DearArchive] Failed to fetch result page for {Date}", date);
            return results;
        }

        foreach (var drawTime in neededDrawTimes)
        {
            var slot = drawTime switch { "1 PM" => "1pm", "6 PM" => "6pm", "8 PM" => "8pm", _ => null };
            if (slot is null) continue;

            var sectionMatch = Regex.Match(html, $@"id=""{slot}""[\s\S]*?class=""rb-num"">([^<]+)<");
            if (!sectionMatch.Success)
            {
                _logger.LogInformation("[DearArchive] No result value found for {Date} {DrawTime}", date, drawTime);
                continue;
            }

            var parts = sectionMatch.Groups[1].Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Regex.IsMatch(parts[0], @"^\d{1,3}[A-Z]{1,2}$") || !Regex.IsMatch(parts[1], @"^\d{5}$"))
            {
                _logger.LogWarning("[DearArchive] Unexpected result format for {Date} {DrawTime} - skipped", date, drawTime);
                continue;
            }

            results.Add(new SambadResultDto
            {
                DrawDate = date.ToString("yyyy-MM-dd"),
                DrawTime = drawTime,
                Result = parts[1], // 5-digit string, preserves leading zeros
                Series = parts[0],
            });
        }

        return results;
    }

    private static bool TryParseArchiveDate(string dd, string mm, string yy, out DateOnly date)
    {
        date = default;
        if (!int.TryParse(dd, out var day) || !int.TryParse(mm, out var month) || !int.TryParse(yy, out var yearSuffix)) return false;
        try
        {
            date = new DateOnly(2000 + yearSuffix, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>Hands already-fetched-and-parsed results to the existing sync pipeline for one date, so
    /// insertion goes through the same duplicate-check/notify path as every other provider.</summary>
    private sealed class PreloadedResultProvider : IResultProvider
    {
        private readonly List<SambadResultDto> _results;
        public string SourceName { get; }

        public PreloadedResultProvider(List<SambadResultDto> results, string sourceName)
        {
            _results = results;
            SourceName = sourceName;
        }

        public Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default) =>
            Task.FromResult(new SambadFetchResult { Success = true, Results = _results });
    }
}
