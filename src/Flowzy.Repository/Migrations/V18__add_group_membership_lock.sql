-- Leader-controlled lock that freezes group membership changes.
ALTER TABLE student_groups
    ADD COLUMN IF NOT EXISTS is_locked BOOLEAN NOT NULL DEFAULT FALSE;
