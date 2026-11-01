# Operations and Monitoring validation

Verification date: 2026-11-01. Runtime: .NET SDK 10.0.401.
Database: isolated PostgreSQL 16.15 on localhost; no cloud deployment is claimed.

## Executable checks

`OperationsMonitoringService.Tests` completed **45 tests, 45 passed, 0 failed,
0 skipped** with MONITORING_TEST_POSTGRES configured. The suite consists of 39
unit/HTTP cases and six actual PostgreSQL cases. Its Gherkin file remains an
acceptance specification, not an executable SpecFlow suite.

The cases cover required fields, malformed UUIDs and dates, timezone offsets,
fractional numeric boundaries, invalid queries, threshold equality, retry
identity, conflicts, alert aggregation, cattle/status filters, pagination and
the published OpenAPI resource. PostgreSQL cases specifically cover:

1. Persisted values and retry identity after reopening the DbContext, including
   more than two decimal places and microsecond timestamp normalization.
2. Eight concurrent readings for one bovine: one alert and readingCount eight.
3. Eight concurrent identical retries: exactly one reading and one alert update.
4. Conflicting retry keys across animals: one success, one conflict, no orphan alert.
5. A concurrent writer that bypasses advisory locks: recover the committed reading
   after rolling back and detaching the failed transaction.
6. Staging HTTP access: missing/wrong API key rejected; correct key admits both
   telemetry and alert access; health remains available.

## Real HTTP process and persistence

A separate Release process used PostgreSQL with Database:UseInMemory=false.
Fourteen requests verified health, creation, retry, conflict, invalid fractional
temperature/GPS, alert grouping, paging, invalid offset/status and threshold
equality. The process was then stopped and started again. A subsequent GET
returned the same alert ID and readingCount two; retrying the original POST
returned 200 and the same reading ID with duplicate=true.

The versioned OpenAPI 3.0.3 contract passed structural validation using
openapi-spec-validator. The served contract is copied by dotnet publish and
available independently of the Swagger UI.

## Reproduce

Run from the repository root with .NET 10 and an isolated PostgreSQL database:

```powershell
$env:MONITORING_TEST_POSTGRES = '<Npgsql connection string for your test database>'
dotnet test OperationsMonitoringService.Tests/OperationsMonitoringService.Tests.csproj `
  --logger 'trx;LogFileName=monitoring.trx'
```

To exercise the real HTTP host with local Swagger:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__UseInMemory = 'false'
$env:ConnectionStrings__Monitoring = '<Npgsql connection string for your database>'
dotnet run --project OperationsMonitoringService/OperationsMonitoringService.csproj `
  --no-launch-profile --urls http://127.0.0.1:5187
```

Open `/swagger` or import `/openapi/monitoring.yaml` into an API client. Submit a
request like OperationsMonitoringService.http, save readingId and alertId,
restart the host and repeat the POST/GET for the same capture. Use your own
credentials via environment variables; never commit them.

## Limits

These checks validate the two monitoring endpoints and their own persistence.
They do not certify IoT hardware, notification delivery, clinical diagnosis,
role/ranch authorization, service-to-service inventory checks, cloud hosting or
full-production migrations. Offset paging is not a stable snapshot during
concurrent insertion. In-memory tests alone do not prove relational behavior.
The published Report2 document was not changed by this implementation task.
