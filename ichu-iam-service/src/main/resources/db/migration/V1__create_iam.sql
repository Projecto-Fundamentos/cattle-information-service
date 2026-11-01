CREATE TABLE iam.accounts (
    id UUID PRIMARY KEY,
    email VARCHAR(254) NOT NULL,
    password_hash VARCHAR(100) NOT NULL,
    status VARCHAR(32) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uq_accounts_email UNIQUE (email),
    CONSTRAINT ck_account_status CHECK (status IN ('PENDING_VERIFICATION', 'ACTIVE'))
);
CREATE TABLE iam.ranch_units (
    id UUID PRIMARY KEY,
    owner_id UUID NOT NULL REFERENCES iam.accounts(id),
    name VARCHAR(120) NOT NULL,
    region VARCHAR(100) NOT NULL,
    district VARCHAR(100) NOT NULL,
    estimated_head_count INTEGER NOT NULL CHECK (estimated_head_count >= 0),
    latitude NUMERIC(9,6) CHECK (latitude BETWEEN -90 AND 90),
    longitude NUMERIC(9,6) CHECK (longitude BETWEEN -180 AND 180),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uq_ranch_owner UNIQUE (owner_id),
    CONSTRAINT ck_coordinates CHECK ((latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL))
);
