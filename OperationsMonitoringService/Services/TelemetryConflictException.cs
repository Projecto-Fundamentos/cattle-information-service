namespace OperationsMonitoringService.Services;

public sealed class TelemetryConflictException()
    : Exception("A reading with this deviceId and recordedAt already exists with different values.");
