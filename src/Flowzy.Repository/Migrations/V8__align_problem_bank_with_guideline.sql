ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS macro_domain VARCHAR(255);
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS sub_domain TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS typical_examples TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS primary_discipline VARCHAR(255);
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS supporting_disciplines TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS best_sources TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS student_capabilities TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS potential_outputs TEXT;
ALTER TABLE problem_domains ADD COLUMN IF NOT EXISTS notes TEXT;

UPDATE problem_domains
SET macro_domain = COALESCE(macro_domain, name),
    sub_domain = COALESCE(sub_domain, description)
WHERE macro_domain IS NULL OR sub_domain IS NULL;

ALTER TABLE problems ALTER COLUMN domain_id DROP NOT NULL;
ALTER TABLE problems ADD COLUMN IF NOT EXISTS owner_lab VARCHAR(255);
ALTER TABLE problems ADD COLUMN IF NOT EXISTS suggested_courses TEXT;
ALTER TABLE problems ADD COLUMN IF NOT EXISTS drive_folder_link VARCHAR(500);

CREATE INDEX IF NOT EXISTS idx_problems_difficulty_level ON problems(difficulty_level);
CREATE INDEX IF NOT EXISTS idx_problems_expected_output ON problems(expected_output);

ALTER TABLE problem_evaluation_criteria ADD COLUMN IF NOT EXISTS code VARCHAR(30);

UPDATE problem_evaluation_criteria
SET code = CONCAT('CRIT-', id)
WHERE code IS NULL OR code = '';

ALTER TABLE problem_evaluation_criteria ALTER COLUMN code SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_problem_evaluation_criteria_code'
          AND conrelid = 'problem_evaluation_criteria'::regclass
    ) THEN
        ALTER TABLE problem_evaluation_criteria
            ADD CONSTRAINT uq_problem_evaluation_criteria_code UNIQUE (code);
    END IF;
END;
$$;
