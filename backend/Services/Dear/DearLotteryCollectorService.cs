using System.Net;
using System.Text.RegularExpressions;
using LotteryAnalytics.Api.Services.Results;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace LotteryAnalytics.Api.Services.Dear;

/// <summary>
/// Fetches Nagaland State Lotteries "Dear" result PDFs archived at 7dear.in
/// (https://www.7dear.in/sambad_lottery_history_result) and extracts the published
/// Series + winning Number for the 1 PM / 6 PM / 8 PM draws. Only ever reports results
/// that are actually present on the site — never guesses or fabricates a value.
/// </summary>
public class DearLotteryCollectorService : IResultProvider
{
    // 7dear's archive naming convention: /media/dear/{yyyy}/{MM}/{Prefix}{ddMMyy}.PDF
    private static readonly (string DrawTime, string Prefix)[] DrawSlots =
    {
        ("1 PM", "MD"),
        ("6 PM", "DD"),
        ("8 PM", "ED"),
    };

    // Matches the 1st-prize "Series Number" pair as printed, e.g. "55C 28021".
    private static readonly Regex WinningNumberPattern = new(@"\b(\d{1,3}[A-Z]{1,2})\s+(\d{5})\b", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly DearOptions _options;
    private readonly ILogger<DearLotteryCollectorService> _logger;

    public DearLotteryCollectorService(HttpClient http, IOptions<DearOptions> options, ILogger<DearLotteryCollectorService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string SourceName => "7Dear";

    public async Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default)
    {
        var results = new List<SambadResultDto>();

        foreach (var (drawTime, prefix) in DrawSlots)
        {
            ct.ThrowIfCancellationRequested();

            var parsed = await FetchOneAsync(date, drawTime, prefix, ct);
            if (parsed is not null) results.Add(parsed);

            if (_options.RequestDelayMs > 0)
            {
                await Task.Delay(_options.RequestDelayMs, ct);
            }
        }

        return new SambadFetchResult { Success = true, Results = results };
    }

    private async Task<SambadResultDto?> FetchOneAsync(DateOnly date, string drawTime, string prefix, CancellationToken ct)
    {
        var path = $"/media/dear/{date:yyyy}/{date:MM}/{prefix}{date:ddMMyy}.PDF";
        byte[] pdfBytes;

        try
        {
            using var response = await _http.GetAsync(path, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("7Dear: no result published yet for {Date} {DrawTime}", date, drawTime);
                return null; // genuinely unavailable — not an error, never retried as one
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("7Dear: unexpected status {StatusCode} for {Date} {DrawTime}", response.StatusCode, date, drawTime);
                return null;
            }

            pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("7Dear: request timed out for {Date} {DrawTime}", date, drawTime);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "7Dear: network error for {Date} {DrawTime}", date, drawTime);
            return null;
        }

        // Some 200 responses are an HTML "not found" placeholder rather than a real PDF —
        // that's still "genuinely unavailable", not a parse error.
        if (pdfBytes.Length < 5 || pdfBytes[0] != '%' || pdfBytes[1] != 'P' || pdfBytes[2] != 'D' || pdfBytes[3] != 'F')
        {
            _logger.LogInformation("7Dear: no result published yet for {Date} {DrawTime} (non-PDF response)", date, drawTime);
            return null;
        }

        return TryParse(pdfBytes, date, drawTime);
    }

    private SambadResultDto? TryParse(byte[] pdfBytes, DateOnly date, string drawTime)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            if (document.NumberOfPages == 0) return null;

            var page = document.GetPage(1);

            // Reconstruct approximate visual reading order (top-to-bottom, left-to-right) from
            // word bounding boxes — raw PDF content-stream order does not reliably match layout.
            var text = string.Join(" ", page.GetWords()
                .OrderByDescending(w => Math.Round(w.BoundingBox.Bottom / 5) * 5)
                .ThenBy(w => w.BoundingBox.Left)
                .Select(w => w.Text));

            // PdfPig tokenizes adjacent glyphs (e.g. "01" "/" "05" "/" "25") as separate words, which
            // our single-space join can pull apart from a literal "dd/MM/yy" substring — match with
            // tolerant whitespace around separators instead of a strict Contains check.
            // (The 1PM/6PM/8PM badge is a raster image in these PDFs, not selectable text, so the draw
            // time itself can't be cross-checked from content — it's already known from which archive
            // URL slot [MD/DD/ED] we requested, so no guessing is involved.)
            var dateRegex = new Regex($@"{date:dd}\s*/\s*{date:MM}\s*/\s*{date:yy}");
            if (!dateRegex.IsMatch(text))
            {
                _logger.LogWarning("7Dear: PDF for {Date} {DrawTime} did not contain the expected date — skipped.", date, drawTime);
                return null;
            }

            // Bound the winning-number search to the header section (draw name/number/date through the
            // winning Series+Number, which is immediately followed by "Sold by :" on every sheet) — this
            // avoids matching unrelated "Series Number"-shaped tokens in promo sections lower on the page.
            var boundaryIdx = text.IndexOf("Sold by", StringComparison.OrdinalIgnoreCase);
            var searchText = boundaryIdx > 0 ? text[..boundaryIdx] : text;

            var match = WinningNumberPattern.Match(searchText);
            if (!match.Success)
            {
                _logger.LogWarning("7Dear: could not locate a winning Series/Number in the PDF for {Date} {DrawTime} — skipped.", date, drawTime);
                return null;
            }

            return new SambadResultDto
            {
                DrawDate = date.ToString("yyyy-MM-dd"),
                DrawTime = drawTime,
                Result = match.Groups[2].Value, // 5-digit number, kept as string to preserve leading zeros
                Series = match.Groups[1].Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "7Dear: failed to parse PDF for {Date} {DrawTime} — skipped.", date, drawTime);
            return null;
        }
    }
}
