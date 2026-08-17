using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.DTOs;
using LotteryAnalytics.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LotteryAnalytics.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AdminAuthController(AppDbContext db, IJwtTokenService tokenService, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid username or password."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = _tokenService.GenerateToken(user);
        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        }));
    }
}
