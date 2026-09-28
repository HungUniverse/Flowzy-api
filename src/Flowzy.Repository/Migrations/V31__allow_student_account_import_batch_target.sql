ALTER TABLE import_batches DROP CONSTRAINT IF EXISTS chk_import_batches_target;
ALTER TABLE import_batches DROP CONSTRAINT IF EXISTS import_batches_target_type_check;

ALTER TABLE import_batches
    ADD CONSTRAINT chk_import_batches_target
    CHECK (target_type IN ('STUDENT', 'STUDENT_ACCOUNT', 'MENTOR', 'PROBLEM_BANK'));
