namespace OperationsMonitoringService.Contracts;

public sealed record AlertResponse(
    Guid Id,
    Guid CattleId,
    string Type,
    string Status,
    decimal Temperature,
    decimal Threshold,
    int ReadingCount,
    DateTime StartedAtUtc,
    DateTime LastObservedAtUtc);
