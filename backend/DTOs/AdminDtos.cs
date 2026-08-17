namespace LotteryAnalytics.Api.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class DashboardSummary
{
    public int TotalResults { get; set; }
    public DateTime? LatestSyncAt { get; set; }
    public bool LatestSyncSuccess { get; set; }
    public string? LatestSyncMessage { get; set; }
    public int SyncLogCount { get; set; }
}

public class SyncLogDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public int RecordsImported { get; set; }
    public string? Message { get; set; }
    public string Trigger { get; set; } = string.Empty;
}
