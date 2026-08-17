using LotteryAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryAnalytics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LotteryResult> LotteryResults => Set<LotteryResult>();
    public DbSet<Draw> Draws => Set<Draw>();
    public DbSet<AnalysisSnapshot> AnalysisSnapshots => Set<AnalysisSnapshot>();
    public DbSet<NotificationSubscription> NotificationSubscriptions => Set<NotificationSubscription>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<PredictionRecord> PredictionRecords => Set<PredictionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var result = modelBuilder.Entity<LotteryResult>();
        result.HasIndex(r => r.DrawDate);
        result.HasIndex(r => r.DrawTime);
        result.HasIndex(r => new { r.DrawDate, r.DrawTime }).IsUnique();
        result.Property(r => r.DrawTime).HasMaxLength(10);
        result.Property(r => r.ResultValue).HasMaxLength(10);
        result.Property(r => r.Series).HasMaxLength(20);
        result.Property(r => r.Source).HasMaxLength(50);

        modelBuilder.Entity<Draw>().HasIndex(d => d.Name).IsUnique();

        modelBuilder.Entity<AnalysisSnapshot>()
            .HasIndex(a => new { a.DrawTime, a.SnapshotType });

        modelBuilder.Entity<AppSetting>().HasIndex(s => s.Key).IsUnique();

        modelBuilder.Entity<AdminUser>().HasIndex(u => u.Username).IsUnique();

        var prediction = modelBuilder.Entity<PredictionRecord>();
        prediction.HasIndex(p => p.DrawDate);
        prediction.HasIndex(p => p.DrawTime);
        prediction.HasIndex(p => new { p.DrawDate, p.DrawTime, p.DigitLength, p.ModelVersion }).IsUnique();
        prediction.Property(p => p.DrawTime).HasMaxLength(10);
        prediction.Property(p => p.ModelVersion).HasMaxLength(20);
        prediction.Property(p => p.ActualResult).HasMaxLength(10);
        prediction.HasOne(p => p.LotteryResult)
            .WithMany()
            .HasForeignKey(p => p.LotteryResultId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Draw>().HasData(
            new Draw { Id = 1, Name = "1 PM", ScheduledTime = new TimeSpan(13, 0, 0) },
            new Draw { Id = 2, Name = "6 PM", ScheduledTime = new TimeSpan(18, 0, 0) },
            new Draw { Id = 3, Name = "8 PM", ScheduledTime = new TimeSpan(20, 0, 0) }
        );
    }
}
