namespace OperationsMonitoringService.Data;

public sealed class TelemetryReading
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid CattleId { get; set; }
    public decimal BodyTemperature { get; set; }
    public decimal ActivityIndex { get; set; }
    public int? RuminationMinutes { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal? BatteryLevel { get; set; }
    public string Origin { get; set; } = "DirectFromDevice";
    public DateTime RecordedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public Guid? AlertId { get; set; }
}
