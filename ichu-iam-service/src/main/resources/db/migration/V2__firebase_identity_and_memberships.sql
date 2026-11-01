ALTER TABLE iam.accounts ALTER COLUMN password_hash DROP NOT NULL;
ALTER TABLE iam.accounts ADD COLUMN external_uid VARCHAR(128);
ALTER TABLE iam.accounts ADD CONSTRAINT uq_accounts_external_uid UNIQUE (external_uid);
ALTER TABLE iam.accounts ADD COLUMN role VARCHAR(20) NOT NULL DEFAULT 'RanchManager';
ALTER TABLE iam.accounts ADD CONSTRAINT ck_account_role CHECK (role IN ('RanchManager','FieldOperator','Veterinarian'));
ALTER TABLE iam.accounts DROP CONSTRAINT ck_account_status;
ALTER TABLE iam.accounts ADD CONSTRAINT ck_account_status CHECK (status IN ('PENDING_VERIFICATION','ACTIVE','TEMPORARILY_BLOCKED'));
CREATE TABLE iam.ranch_membership (
    id UUID PRIMARY KEY,
    user_account_id UUID NOT NULL REFERENCES iam.accounts(id),
    ranch_id UUID NOT NULL REFERENCES iam.ranch_units(id),
    membership_type VARCHAR(20) NOT NULL CHECK (membership_type IN ('Owner','Operator')),
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uq_membership_account_ranch UNIQUE (user_account_id, ranch_id)
);
INSERT INTO iam.ranch_membership (id,user_account_id,ranch_id,membership_type,active,created_at)
    SELECT id,owner_id,id,'Owner',TRUE,created_at FROM iam.ranch_units;
ALTER TABLE iam.ranch_units DROP CONSTRAINT uq_ranch_owner;
CREATE INDEX ix_memberships_ranch ON iam.ranch_membership(ranch_id);
ALTER TABLE iam.ranch_units ADD COLUMN name_key VARCHAR(120);
UPDATE iam.ranch_units SET name_key = LOWER(TRIM(name));
ALTER TABLE iam.ranch_units ALTER COLUMN name_key SET NOT NULL;
ALTER TABLE iam.ranch_units ADD CONSTRAINT uq_owner_ranch_name UNIQUE (owner_id,name_key);
