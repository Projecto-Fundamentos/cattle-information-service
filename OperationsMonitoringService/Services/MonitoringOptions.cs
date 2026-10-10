namespace OperationsMonitoringService.Services;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public decimal FeverThresholdCelsius { get; set; } = 39.5m;
}
