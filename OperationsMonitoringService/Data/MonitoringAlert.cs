namespace OperationsMonitoringService.Data;

public sealed class MonitoringAlert
{
    public Guid Id { get; set; }
    public Guid CattleId { get; set; }
    public Guid TriggerReadingId { get; set; }
    public string Type { get; set; } = "FEVER";
    public string Status { get; set; } = "ACTIVE";
    public decimal ThresholdCelsius { get; set; }
    public decimal MaximumTemperatureCelsius { get; set; }
    public int ReadingCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastObservedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
