using System.Text;
using LotteryAnalytics.Api.Common;
using LotteryAnalytics.Api.Data;
using LotteryAnalytics.Api.Models;
using LotteryAnalytics.Api.Services.Analysis;
using LotteryAnalytics.Api.Services.Auth;
using LotteryAnalytics.Api.Services.Dear;
using LotteryAnalytics.Api.Services.Notifications;
using LotteryAnalytics.Api.Services.Predictions;
using LotteryAnalytics.Api.Services.Results;
using LotteryAnalytics.Api.Services.Sambad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms like Render assign a port at runtime via the PORT env var and expect the
// app to bind to 0.0.0.0:$PORT. Local dev is untouched — launchSettings.json / ASPNETCORE_URLS
// still control the port when PORT isn't set (which it normally isn't on a dev machine).
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Config binding falls back to env vars (ConnectionStrings__DefaultConnection, Sambad__Token, etc.)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<SambadOptions>(builder.Configuration.GetSection(SambadOptions.SectionName));
builder.Services.AddHttpClient<ISambadApiClient, SambadApiClient>((sp, client) =>
    {
        var opts = builder.Configuration.GetSection(SambadOptions.SectionName).Get<SambadOptions>() ?? new SambadOptions();
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl)) client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 15);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

builder.Services.Configure<DearOptions>(builder.Configuration.GetSection(DearOptions.SectionName));
var dearOpts = builder.Configuration.GetSection(DearOptions.SectionName).Get<DearOptions>() ?? new DearOptions();
builder.Services.AddHttpClient<DearLotteryCollectorService>(client =>
    {
        client.BaseAddress = new Uri(dearOpts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(dearOpts.TimeoutSeconds > 0 ? dearOpts.TimeoutSeconds : 20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LotteryAnalytics-DearCollector/1.0 (+historical result import)");
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx/408 — NOT 404, which means "not yet published"
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
builder.Services.AddScoped<IDearBackfillService, DearBackfillService>();

// Separate site (dearlottery.in) from the 7dear.in PDF collector above — its archive page is
// the source of truth for which date+draw links exist.
builder.Services.AddHttpClient<DearArchiveCollectorService>(client =>
    {
        client.BaseAddress = new Uri(dearOpts.ArchiveBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(dearOpts.TimeoutSeconds > 0 ? dearOpts.TimeoutSeconds : 20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LotteryAnalytics-DearArchiveCollector/1.0 (+historical result import)");
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

// Result provider priority: Sambad (if configured) > 7Dear (if enabled) > Manual admin entry (default).
// No external calls are made unless a provider is explicitly configured/enabled.
var sambadOpts = builder.Configuration.GetSection(SambadOptions.SectionName).Get<SambadOptions>() ?? new SambadOptions();
var sambadConfigured = !string.IsNullOrWhiteSpace(sambadOpts.BaseUrl) && !string.IsNullOrWhiteSpace(sambadOpts.Token);
if (sambadConfigured)
{
    builder.Services.AddScoped<IResultProvider, SambadApiProvider>();
}
else if (dearOpts.Enabled && dearOpts.DailyCheckEnabled)
{
    builder.Services.AddScoped<IResultProvider>(sp => sp.GetRequiredService<DearLotteryCollectorService>());
}
else
{
    builder.Services.AddScoped<IResultProvider, ManualResultProvider>();
}
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IManualResultService, ManualResultService>();
builder.Services.AddScoped<IHistoricalImportService, HistoricalImportService>();
builder.Services.AddHostedService<DearDrawScheduleService>();

builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.SectionName));
builder.Services.AddScoped<INotificationService, WebPushNotificationService>();
builder.Services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();
builder.Services.AddHostedService<DailyReminderBackgroundService>();

builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddHostedService<SyncBackgroundService>();
builder.Services.AddHostedService<SambadDrawScheduleService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.Configure<ScoringWeights>(builder.Configuration.GetSection(ScoringWeights.SectionName));
builder.Services.AddScoped<ICandidateScoringService, CandidateScoringService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddScoped<IModelEvaluationService, ModelEvaluationService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AdminBootstrapOptions>(builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrEmpty(jwtOptions.Secret) ? "dev-only-placeholder-secret-32-chars-min" : jwtOptions.Secret))
        };
    });
builder.Services.AddAuthorization();

// Allowed origins come from the Cors:AllowedOrigins config array (Cors__AllowedOrigins__0=... as env
// vars) plus an optional single FRONTEND_URL env var, for the deployed Vercel domain. The
// appsettings.json base default (localhost, for local dev) is dropped outside Development so a
// missing FRONTEND_URL/Cors__AllowedOrigins__0 on the host can't silently fall back to localhost.
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOrigins = builder.Environment.IsDevelopment()
    ? configuredOrigins.ToList()
    : configuredOrigins.Where(o => !o.Contains("localhost", StringComparison.OrdinalIgnoreCase)).ToList();
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    var trimmed = frontendUrl.TrimEnd('/');
    if (!allowedOrigins.Contains(trimmed)) allowedOrigins.Add(trimmed);
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Render sits in front of the app on a private network; there's no fixed proxy IP to pin here.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness + DB connectivity check. Returns 200 only when the API and the database are both
// reachable; never returns connection strings, server names, or exception details.
app.MapGet("/api/health", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "ok" })
            : Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

await SeedAdminUserAsync(app);

app.Run();

static async Task SeedAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var bootstrap = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminBootstrapOptions>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (await db.AdminUsers.AnyAsync()) return;

    if (string.IsNullOrWhiteSpace(bootstrap.Username) || string.IsNullOrWhiteSpace(bootstrap.Password))
    {
        logger.LogWarning("No admin user exists and AdminBootstrap:Username/Password are not configured. Set them to create the first admin account.");
        return;
    }

    db.AdminUsers.Add(new AdminUser
    {
        Username = bootstrap.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(bootstrap.Password)
    });
    await db.SaveChangesAsync();
    logger.LogInformation("Seeded initial admin user '{Username}'.", bootstrap.Username);
}

public partial class Program { }
