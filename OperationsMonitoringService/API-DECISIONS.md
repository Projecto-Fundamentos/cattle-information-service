# API contract decisions

The contract in `openapi.yaml` was defined before this increment's implementation.
The reference is the current [Zalando RESTful API and Event Guidelines](https://opensource.zalando.com/restful-api-guidelines/),
including API First, data formats, HTTP semantics, retry keys, errors, security,
pagination, compatibility and the pagination/concurrency appendices.

## Resources and permissions

`POST /api/v1/telemetry` accepts one capture, and `GET /api/v1/alerts` lists
matching fever alerts. `/health` and `/openapi/monitoring.yaml` are operational
and documentation resources. Identifiers are UUIDs, numeric limits are explicit,
and timestamps require an offset and are normalized to UTC.

The existing service API key outside Development is preserved and documented.
It does not provide user, role, bovine existence or ranch authorization. These
require agreed IAM and inventory contracts; this increment does not invent them.

## Compatibility

- Preserve `/api/v1`, camelCase properties, the top-level alert array and the
  existing `201`, `200`, `400`, `401` and `409` behavior. These conventions differ
  from some Zalando rules but are already used by this project.
- Preserve `{ "error": "..." }` for manually handled query/conflict errors and
  Problem Details for automatic validation. The OpenAPI contract describes both
  shapes accurately. Changing published errors needs consumer agreement.
- Add `offset=0` as an optional paging input, keep the existing default limit 100,
  and add an ID tie breaker. Offset pages can shift during concurrent inserts.
  Cursor pagination is preferable for a future agreed contract; the appendices
  provide guidance, not a reason to break the current response format.
- Enforce existing numeric bounds using their actual numeric types. The old
  integer Range attribute rounded fractional inputs before comparing them.
  Fixing it does not redefine the documented bounds.
- Require timezone information as specified by OpenAPI date-time. Equivalent
  offsets identify the same capture. PostgreSQL microsecond timestamp precision
  is explicit; the retry key never relies on floating-point timestamps.
- Preserve accepted decimal values in unconstrained PostgreSQL numeric columns.
  Rounding to two decimal places previously caused false conflicts on retries.

## HTTP behavior and concurrency

A new reading returns 201. An identical retry returns 200 and its existing
readingId, with duplicate=true and alertRaised=false. A changed retry returns
409. A new reading within an existing six-hour alert window still returns 201
because the reading is new; alertRaised=false means the alert was reused.

There is no reading retrieval endpoint, so the implementation does not invent
a nonfunctional Location URL. Idempotency-Key and ETag/If-Match are options from
the guide, not necessary headers for these two existing operations. No update
endpoint or optimistic-locking protocol is claimed.

PostgreSQL locks are infrastructure concerns and live in MonitoringWriteScope;
the fever threshold/window remain in TelemetryIngestionService. Each ingestion
transaction locks the capture key and bovine in a global order. Failed unique
inserts roll back and detach the transaction before an idempotent retry lookup.
Provider failures unrelated to a unique-key race are not disguised as duplicates.

MUST, SHOULD and MAY distinguish obligations, recommendations and options within
Zalando's guide. They are not all universal REST requirements. This increment
does not claim full guide compliance or add Zalando infrastructure, domains or
proprietary headers.

## Persistence upgrade

Fresh isolated monitoring databases use the current EnsureCreated mechanism.
Existing monitoring databases require the explicit widening script
`upgrade-precision.sql`; startup does not silently alter an existing schema.
Managed versioned migrations and IAM integration remain prerequisites for a
production rollout. Development mode and an in-memory health response do not
demonstrate durable deployment.

## Primary technical references

- [PostgreSQL numeric types](https://www.postgresql.org/docs/16/datatype-numeric.html)
- [PostgreSQL advisory locks](https://www.postgresql.org/docs/16/explicit-locking.html#ADVISORY-LOCKS)
- [Zalando guide](https://opensource.zalando.com/restful-api-guidelines/)
