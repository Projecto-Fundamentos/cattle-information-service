namespace OperationsMonitoringService.Contracts;

// Preserve the published { "error": "..." } shape for manual query/conflict errors.
public sealed record ApiError(string Error);
