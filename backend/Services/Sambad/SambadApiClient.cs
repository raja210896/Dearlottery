using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Services.Sambad;

/// <summary>
/// Backend-only client for the Sambad results API. Credentials never leave the server.
/// Follows Sambad's published terms: reads only permitted result endpoints, no scraping,
/// no bypassing of access restrictions.
/// </summary>
public class SambadApiClient : ISambadApiClient
{
    private readonly HttpClient _http;
    private readonly SambadOptions _options;
    private readonly ILogger<SambadApiClient> _logger;

    public SambadApiClient(HttpClient http, IOptions<SambadOptions> options, ILogger<SambadApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SambadFetchResult> FetchResultsAsync(DateOnly date, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("Sambad API not configured (missing base URL or token).");
            return new SambadFetchResult { Success = false, Error = "Sambad API is not configured." };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"results?date={date:yyyy-MM-dd}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Sambad API returned {StatusCode} for {Date}", response.StatusCode, date);
                return new SambadFetchResult { Success = false, Error = $"Sambad API error: {(int)response.StatusCode}" };
            }

            var payload = await response.Content.ReadFromJsonAsync<List<SambadResultDto>>(cancellationToken: ct);
            if (payload is null)
            {
                return new SambadFetchResult { Success = false, Error = "Empty or invalid response from Sambad API." };
            }

            var valid = payload.Where(IsValid).ToList();
            if (valid.Count != payload.Count)
            {
                _logger.LogWarning("Dropped {Count} invalid Sambad records for {Date}", payload.Count - valid.Count, date);
            }

            return new SambadFetchResult { Success = true, Results = valid };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError("Sambad API request timed out for {Date}", date);
            return new SambadFetchResult { Success = false, Error = "Sambad API request timed out." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Sambad API network error for {Date}", date);
            return new SambadFetchResult { Success = false, Error = "Unable to reach Sambad API." };
        }
    }

    private static bool IsValid(SambadResultDto dto) =>
        !string.IsNullOrWhiteSpace(dto.DrawTime) &&
        !string.IsNullOrWhiteSpace(dto.Result) &&
        DateOnly.TryParse(dto.DrawDate, out _) &&
        dto.Result.All(char.IsDigit);
}
