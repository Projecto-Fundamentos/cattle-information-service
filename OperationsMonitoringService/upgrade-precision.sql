-- Existing databases created by the original EnsureCreated implementation only.
-- Run once before deploying this revision. These changes widen precision and
-- preserve existing values; previously rounded digits cannot be recovered.
BEGIN;
ALTER TABLE telemetry_readings
    ALTER COLUMN "BodyTemperature" TYPE numeric,
    ALTER COLUMN "ActivityIndex" TYPE numeric,
    ALTER COLUMN "BatteryLevel" TYPE numeric;
ALTER TABLE monitoring_alerts
    ALTER COLUMN "ThresholdCelsius" TYPE numeric,
    ALTER COLUMN "MaximumTemperatureCelsius" TYPE numeric;
COMMIT;
