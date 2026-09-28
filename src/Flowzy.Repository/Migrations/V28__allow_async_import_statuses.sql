ALTER TABLE import_batches DROP CONSTRAINT IF EXISTS chk_import_batches_status;
ALTER TABLE import_batches DROP CONSTRAINT IF EXISTS import_batches_status_check;

ALTER TABLE import_batches
    ADD CONSTRAINT chk_import_batches_status
    CHECK (status IN ('QUEUED', 'RUNNING', 'COMPLETED', 'FAILED'));
