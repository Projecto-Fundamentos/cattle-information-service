# Operations & Monitoring service

Sprint 1 implementation of the ICHU monitoring API. This project is separate from `CattleInformationService` in the same repository.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/v1/telemetry` | Validate and store temperature, activity and GPS telemetry. A reading above the configured fever threshold creates or updates an alert. |
| `GET` | `/api/v1/alerts` | List active fever alerts. Optional `cattleId`, `status=ACTIVE|ALL` and `limit=1..100` query parameters. |
| `GET` | `/health` | Check database connectivity. |

Swagger UI is available at `/swagger` in the `Development` environment. A sample request is in [`OperationsMonitoringService.http`](OperationsMonitoringService.http).

`POST /api/v1/telemetry` requires `deviceId`, `cattleId`, `bodyTemperature` (30–45 °C), `activityIndex` (0–100), `latitude`, `longitude`, and ISO 8601 `recordedAt`. `ruminationMinutes`, `batteryLevel`, and `origin` are optional. The response is `201 Created` for a new reading, `200 OK` for an identical retry with the same `(deviceId, recordedAt)`, `409 Conflict` if that key is reused with different values, and `400 Bad Request` for invalid input. An identical retry returns the original `readingId` and does not create another alert.

The initial fever threshold is **39.5 °C** (`Monitoring:FeverThresholdCelsius`). Readings for the same animal and alert type within six hours of the first alert update that alert rather than opening a duplicate. Both capture time and server receipt time are stored. This is a configurable project rule, not a veterinary diagnosis.

## Run locally

Requires .NET 10 SDK.

```powershell
dotnet run --project OperationsMonitoringService/OperationsMonitoringService.csproj
```

The Development profile uses an **in-memory demonstration database** so Swagger works without infrastructure. Its data disappears when the process stops. Tests also use an isolated in-memory store.

For a persistent deployment, provide PostgreSQL through `ConnectionStrings__Monitoring` and run outside `Development`:

```text
ConnectionStrings__Monitoring=Host=localhost;Port=5432;Database=monitoring;Username=monitoring;Password=<your-secret>
Security__ApiKey=<your-secret>
```

Outside Development, `/api` requests require the `X-API-Key` header. Do not commit real credentials. Schema creation currently uses `EnsureCreated`; use versioned migrations before evolving a production database.

## Tests and current integration boundary

```powershell
dotnet test CattleInformationService.sln
```

The service does **not yet verify** that a `cattleId` is active or that a `deviceId` is registered; those checks require contracts with the Cattle Information and Identity/Access services. The API key is a temporary service-level safeguard, not user/role authorization. Do not expose this prototype as a complete production monitoring system until those integrations, stage-specific thresholds, notification delivery and migrations are implemented.
