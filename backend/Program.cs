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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.SectionName));
builder.Services.AddScoped<INotificationService, WebPushNotificationService>();
builder.Services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();
builder.Services.AddHostedService<DailyReminderBackgroundService>();

builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddHostedService<SyncBackgroundService>();
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

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
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

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
