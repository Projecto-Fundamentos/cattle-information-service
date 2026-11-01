using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationsMonitoringService.Contracts;
using OperationsMonitoringService.Data;

namespace OperationsMonitoringService.Tests;

public sealed class MonitoringContractTests
{
    private static JsonObject ValidRequest() => new()
    {
        ["deviceId"] = Guid.NewGuid().ToString(),
        ["cattleId"] = Guid.NewGuid().ToString(),
        ["bodyTemperature"] = 40.2m,
        ["activityIndex"] = 12,
        ["latitude"] = -13.656,
        ["longitude"] = -73.388,
        ["recordedAt"] = DateTimeOffset.UtcNow.AddHours(-2).ToString("O"),
        ["origin"] = "EdgeGateway"
    };

    [Theory]
    [InlineData("deviceId", "\"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("cattleId", "\"invalid-uuid\"")]
    [InlineData("bodyTemperature", "29.9")]
    [InlineData("bodyTemperature", "45.1")]
    [InlineData("activityIndex", "null")]
    [InlineData("activityIndex", "-1")]
    [InlineData("activityIndex", "101")]
    [InlineData("activityIndex", "100.1")]
    [InlineData("latitude", "90.1")]
    [InlineData("latitude", "null")]
    [InlineData("longitude", "-180.1")]
    [InlineData("ruminationMinutes", "1441")]
    [InlineData("batteryLevel", "101")]
    [InlineData("batteryLevel", "100.1")]
    [InlineData("origin", "\"InvalidOrigin\"")]
    [InlineData("origin", "null")]
    [InlineData("recordedAt", "\"not-a-date\"")]
    [InlineData("recordedAt", "\"2026-11-01T10:00:00\"")]
    public async Task InvalidTelemetryReturnsValidationProblemAndDoesNotInsert(string field, string json)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = ValidRequest();
        var deviceId = Guid.Parse(request["deviceId"]!.GetValue<string>());
        request[field] = JsonNode.Parse(json);

        var response = await client.PostAsJsonAsync("/api/v1/telemetry", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(400, problem!["status"]!.GetValue<int>());
        Assert.NotNull(problem["errors"]);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<MonitoringDbContext>()
            .Readings.AnyAsync(reading => reading.DeviceId == deviceId));
    }

    [Fact]
    public async Task FutureTimestampIsRejected()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = ValidRequest();
        request["recordedAt"] = DateTimeOffset.UtcNow.AddHours(1).ToString("O");
        var response = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("status=INVALID", "status must be ACTIVE or ALL.")]
    [InlineData("limit=0", "limit must be between 1 and 100.")]
    [InlineData("limit=101", "limit must be between 1 and 100.")]
    [InlineData("offset=-1", "offset must be greater than or equal to zero.")]
    public async Task InvalidQueryPreservesExistingErrorShape(string query, string message)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/alerts?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(message, error!.Error);
    }

    [Theory]
    [InlineData("limit=abc")]
    [InlineData("cattleId=abc")]
    [InlineData("offset=2147483648")]
    public async Task InvalidQueryTypesReturn400(string query)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/alerts?" + query)).StatusCode);
    }

    [Fact]
    public async Task RetriesConflictsAndFurtherReadingsPreserveAlertCount()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = ValidRequest();
        var first = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        var original = await first.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        var retry = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        var repeated = await retry.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(original!.ReadingId, repeated!.ReadingId);
        Assert.True(repeated.Duplicate);

        request["bodyTemperature"] = 40.5m;
        var conflict = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.NotEmpty((await conflict.Content.ReadFromJsonAsync<ApiError>())!.Error);

        request["recordedAt"] = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");
        var next = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        var updated = await next.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        Assert.False(updated!.AlertRaised);
        Assert.Equal(original.AlertId, updated.AlertId);
        var alerts = await client.GetFromJsonAsync<List<AlertResponse>>(
            "/api/v1/alerts?cattleId=" + request["cattleId"]!.GetValue<string>());
        Assert.Single(alerts!);
        Assert.Equal(2, alerts![0].ReadingCount);
        Assert.Equal(40.5m, alerts[0].Temperature);
    }

    [Fact]
    public async Task QueriesFilterStatusAndAnimalAndPageWithoutOverlappingTies()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var cattleId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            foreach (var suffix in new[] { "001", "002", "003" })
                db.Alerts.Add(new MonitoringAlert
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000" + suffix),
                    CattleId = cattleId, ReadingCount = 1, CreatedAtUtc = now,
                    StartedAtUtc = now, LastObservedAtUtc = now,
                    Status = suffix == "001" ? "CLOSED" : "ACTIVE"
                });
            await db.SaveChangesAsync();
        }
        var query = $"/api/v1/alerts?cattleId={cattleId}";
        var first = await client.GetFromJsonAsync<List<AlertResponse>>(query + "&limit=1");
        var second = await client.GetFromJsonAsync<List<AlertResponse>>(query + "&limit=1&offset=1");
        var all = await client.GetFromJsonAsync<List<AlertResponse>>(query + "&status=ALL");
        var other = await client.GetFromJsonAsync<List<AlertResponse>>("/api/v1/alerts?cattleId=" + Guid.NewGuid());
        Assert.Single(first!);
        Assert.Single(second!);
        Assert.NotEqual(first![0].Id, second![0].Id);
        Assert.Equal(3, all!.Count);
        Assert.All(first.Concat(second), alert => Assert.Equal("ACTIVE", alert.Status));
        Assert.Empty(other!);
    }

    [Fact]
    public async Task EquivalentTimezoneOffsetIsAnIdenticalRetry()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = ValidRequest();
        var captureTime = DateTimeOffset.Parse(request["recordedAt"]!.GetValue<string>());
        var first = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        var original = await first.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        request["recordedAt"] = captureTime.ToOffset(TimeSpan.FromHours(-5)).ToString("O");
        var retry = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(original!.ReadingId, (await retry.Content.ReadFromJsonAsync<TelemetryIngestResponse>())!.ReadingId);
    }

    [Fact]
    public async Task ThresholdEqualityDoesNotRaiseAlertAndContractIsPublished()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = ValidRequest();
        request["bodyTemperature"] = 39.5m;
        var response = await client.PostAsJsonAsync("/api/v1/telemetry", request);
        var reading = await response.Content.ReadFromJsonAsync<TelemetryIngestResponse>();
        Assert.False(reading!.AlertRaised);
        Assert.Null(reading.AlertId);
        var contract = await client.GetStringAsync("/openapi/monitoring.yaml");
        Assert.Contains("operationId: ingestTelemetry", contract);
        Assert.Contains("operationId: listAlerts", contract);
        Assert.DoesNotContain("/api/v1/cattle", contract);
    }
}
