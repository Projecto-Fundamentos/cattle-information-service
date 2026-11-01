# Operations & Monitoring service

Service **C. Operations & Monitoring** for ICHU. This executable is independent of
`CattleInformationService`, even though both projects share this repository.

This increment completes and validates the existing implementation. It does not
modify the Cattle Information service or the published Report2 document.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/v1/telemetry` | Validate and store temperature, activity and GPS telemetry. A reading above the configured fever threshold creates or updates an alert. |
| `GET` | `/api/v1/alerts` | List active fever alerts. Optional `cattleId`, `status=ACTIVE|ALL`, `limit=1..100`, and nonnegative `offset` query parameters. |
| `GET` | `/health` | Check database connectivity. |

Swagger UI is available at `/swagger` in the `Development` environment. A sample request is in [`OperationsMonitoringService.http`](OperationsMonitoringService.http).

The versioned contract is [`openapi.yaml`](openapi.yaml), served at
`/openapi/monitoring.yaml` and used by Swagger UI. Read
[`API-DECISIONS.md`](API-DECISIONS.md) for compatibility decisions. No published
route, JSON field, status code or error shape has been renamed.

`POST /api/v1/telemetry` requires `deviceId`, `cattleId`, `bodyTemperature` (30–45 °C), `activityIndex` (0–100), `latitude`, `longitude`, and ISO 8601 `recordedAt`. `ruminationMinutes`, `batteryLevel`, and `origin` are optional. The response is `201 Created` for a new reading, `200 OK` for an identical retry with the same `(deviceId, recordedAt)`, `409 Conflict` if that key is reused with different values, and `400 Bad Request` for invalid input. An identical retry returns the original `readingId` and does not create another alert.

The initial fever threshold is **39.5 °C** (`Monitoring:FeverThresholdCelsius`). Readings for the same animal and alert type within six hours of the first alert update that alert rather than opening a duplicate. Both capture time and server receipt time are stored. This is a configurable project rule, not a veterinary diagnosis.

`recordedAt` must include `Z` or a numeric UTC offset, and cannot exceed server
time by more than five minutes. Capture timestamps are normalized to UTC and
PostgreSQL microseconds for retry identity. Accepted decimals are stored without
rounding to two places. Numeric validation uses decimal/double comparisons, so
45.1 C and latitude 90.1 correctly return 400.

Alert queries run through `AlertQueryService`, outside the HTTP controller.
Results are ordered by creation time and ID descending. `offset` provides an
additive way to retrieve later pages without changing the existing array shape.
Concurrent inserts may shift offset pages; snapshot paging is not promised.

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

To use real PostgreSQL while retaining the local Swagger UI, set both
`ConnectionStrings__Monitoring` and `Database__UseInMemory=false` in Development.
On a **new, dedicated monitoring database**, the existing `EnsureCreated` startup
creates the service tables. On a database created by the old implementation,
apply [`upgrade-precision.sql`](upgrade-precision.sql) once before this revision:

```powershell
psql -v ON_ERROR_STOP=1 -f OperationsMonitoringService/upgrade-precision.sql
```

Before invoking psql, set `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, and
`PGPASSWORD` in your environment for the target monitoring database. The
application's Npgsql semicolon connection string is not a psql connection string.
The SQL widens numeric columns and preserves existing data; it cannot recover
digits already rounded by an earlier release. Full production migration
management remains a separate integration task.

In PostgreSQL, a transaction includes reading insertion and alert creation or
update. Infrastructure acquires transaction-scoped advisory locks for the retry
key and bovine in a consistent order. This prevents duplicate alerts and lost
counts across service instances. The in-memory demo does not certify database
constraints or cross-process concurrency.

## Tests and current integration boundary

```powershell
dotnet test OperationsMonitoringService.Tests/OperationsMonitoringService.Tests.csproj
```

For the six real PostgreSQL integration cases, point the following variable at
an **isolated test database** before running the same command. The suite inserts
randomly identified test records and does not delete data. Without this variable,
PostgreSQL cases are explicitly skipped, not counted as passes.

```text
MONITORING_TEST_POSTGRES=Host=localhost;Port=5432;Database=monitoring_tests;Username=monitoring;Password=<test-secret>
```

The verification on 2026-11-01 passed **45 tests with zero failures or skips**,
including PostgreSQL 16.15. A separate Release HTTP check passed 14 scenarios,
including an application-process restart followed by the same alert and reading
identifiers. See [`VALIDATION.md`](VALIDATION.md) for scope and reproduction.

The service does **not yet verify** that a `cattleId` is active or that a `deviceId` is registered; those checks require contracts with the Cattle Information and Identity/Access services. The API key is a temporary service-level safeguard, not user/role authorization. Do not expose this prototype as a complete production monitoring system until those integrations, stage-specific thresholds, notification delivery and migrations are implemented.
