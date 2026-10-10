namespace OperationsMonitoringService.Contracts;

public sealed record TelemetryIngestResponse(
    Guid ReadingId,
    bool AlertRaised,
    Guid? AlertId,
    string? AlertType,
    bool Duplicate);
