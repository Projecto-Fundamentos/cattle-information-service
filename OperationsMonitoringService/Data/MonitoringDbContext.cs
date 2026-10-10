using Microsoft.EntityFrameworkCore;

namespace OperationsMonitoringService.Data;

public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryReading> Readings => Set<TelemetryReading>();
    public DbSet<MonitoringAlert> Alerts => Set<MonitoringAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryReading>(entity =>
        {
            entity.ToTable("telemetry_readings");
            entity.HasKey(reading => reading.Id);
            entity.HasIndex(reading => new { reading.DeviceId, reading.RecordedAtUtc }).IsUnique();
            entity.HasIndex(reading => new { reading.CattleId, reading.RecordedAtUtc });
            entity.Property(reading => reading.Origin).HasMaxLength(32);
            entity.Property(reading => reading.BodyTemperature).HasPrecision(5, 2);
            entity.Property(reading => reading.ActivityIndex).HasPrecision(5, 2);
            entity.Property(reading => reading.BatteryLevel).HasPrecision(5, 2);
        });

        modelBuilder.Entity<MonitoringAlert>(entity =>
        {
            entity.ToTable("monitoring_alerts");
            entity.HasKey(alert => alert.Id);
            entity.HasIndex(alert => new { alert.CattleId, alert.Status, alert.StartedAtUtc });
            entity.Property(alert => alert.Type).HasMaxLength(32);
            entity.Property(alert => alert.Status).HasMaxLength(24);
            entity.Property(alert => alert.ThresholdCelsius).HasPrecision(5, 2);
            entity.Property(alert => alert.MaximumTemperatureCelsius).HasPrecision(5, 2);
        });
    }
}
