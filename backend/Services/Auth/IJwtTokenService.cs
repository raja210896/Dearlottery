using LotteryAnalytics.Api.Models;

namespace LotteryAnalytics.Api.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(AdminUser user);
}
