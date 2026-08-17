namespace LotteryAnalytics.Api.Services.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LotteryAnalytics";
    public string Audience { get; set; } = "LotteryAnalytics";
    public int ExpiryMinutes { get; set; } = 120;
}

public class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
