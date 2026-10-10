using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OperationsMonitoringService.Contracts;

namespace OperationsMonitoringService.Tests;

public sealed class MonitoringApiTests
{
    [Fact]
    public async Task ApiIngestsTelemetryAndListsTheAlert()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var cattleId = Guid.NewGuid();
        var payload = new TelemetryIngestRequest
        {
            DeviceId = Guid.NewGuid(),
            CattleId = cattleId,
            BodyTemperature = 40.2m,
            ActivityIndex = 12,
            Latitude = -13.656,
            Longitude = -73.388,
            RecordedAt = DateTimeOffset.UtcNow
        };

        var created = await client.PostAsJsonAsync("/api/v1/telemetry", payload);
        var reading = await created.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        var alerts = await client.GetFromJsonAsync<List<AlertResponse>>(
            $"/api/v1/alerts?cattleId={cattleId}");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(reading);
        Assert.NotNull(alerts);
        Assert.True(reading.AlertRaised);
        Assert.Single(alerts);
        Assert.Equal(reading.AlertId, alerts[0].Id);
    }

    [Fact]
    public async Task ApiRejectsInvalidTelemetry()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/telemetry", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
