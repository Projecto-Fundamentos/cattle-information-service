using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OperationsMonitoringService.Contracts;

public sealed class TelemetryIngestRequest : IValidatableObject
{
    public Guid DeviceId { get; init; }
    public Guid CattleId { get; init; }

    [Range(typeof(decimal), "30", "45")]
    public decimal BodyTemperature { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal? ActivityIndex { get; init; }

    [Range(0, 1440)]
    public int? RuminationMinutes { get; init; }

    [Required]
    [Range(-90.0, 90.0)]
    public double? Latitude { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public double? Longitude { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? BatteryLevel { get; init; }

    [MaxLength(32)]
    public string Origin { get; init; } = "DirectFromDevice";

    [JsonConverter(typeof(RecordedAtJsonConverter))]
    public DateTimeOffset RecordedAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeviceId == Guid.Empty)
            yield return new ValidationResult("deviceId is required.", [nameof(DeviceId)]);
        if (CattleId == Guid.Empty)
            yield return new ValidationResult("cattleId is required.", [nameof(CattleId)]);
        if (RecordedAt == default)
            yield return new ValidationResult("recordedAt is required.", [nameof(RecordedAt)]);
        else if (RecordedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            yield return new ValidationResult("recordedAt cannot be more than five minutes in the future.", [nameof(RecordedAt)]);
        if (Origin is not ("DirectFromDevice" or "EdgeGateway"))
            yield return new ValidationResult("origin must be DirectFromDevice or EdgeGateway.", [nameof(Origin)]);
    }
}
