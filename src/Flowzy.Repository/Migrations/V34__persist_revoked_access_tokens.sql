-- Flowzy-only operational state; original Java migrations V1-V33 remain unchanged.
CREATE TABLE revoked_access_token (
    token_hash VARCHAR(64) PRIMARY KEY,
    expires_at TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_revoked_access_token_expires_at ON revoked_access_token (expires_at);
