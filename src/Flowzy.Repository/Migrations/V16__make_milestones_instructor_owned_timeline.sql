-- Convert Phase 10 milestones into generic, instructor-owned timeline items.
ALTER TABLE instructors
    ADD COLUMN IF NOT EXISTS department VARCHAR(150);

ALTER TABLE instructors
    ADD COLUMN IF NOT EXISTS expertise TEXT;

ALTER TABLE course_milestones
    ADD COLUMN IF NOT EXISTS position BIGINT NOT NULL DEFAULT 0;

ALTER TABLE course_milestones
    DROP CONSTRAINT IF EXISTS chk_course_milestones_type;

UPDATE course_milestones
SET type = 'TIMELINE'
WHERE type IN ('CHECKPOINT', 'OUTCOME');

ALTER TABLE course_milestones
    ADD CONSTRAINT chk_course_milestones_type CHECK (type = 'TIMELINE');

ALTER TABLE course_milestones
    DROP CONSTRAINT IF EXISTS uq_course_milestones_term_course_title;

ALTER TABLE course_milestones
    ADD CONSTRAINT uq_course_milestones_owner_term_course_title
        UNIQUE (instructor_id, term, course_code, title);

ALTER TABLE course_milestones
    DROP CONSTRAINT IF EXISTS chk_course_milestones_status;

ALTER TABLE course_milestones
    ADD CONSTRAINT chk_course_milestones_status
        CHECK (status IN ('ACTIVE', 'CLOSED', 'ARCHIVED', 'INACTIVE'));

ALTER TABLE course_milestones
    DROP CONSTRAINT IF EXISTS chk_course_milestones_max_score;

ALTER TABLE course_milestones
    ADD CONSTRAINT chk_course_milestones_max_score CHECK (max_score > 0);

CREATE INDEX IF NOT EXISTS idx_course_milestones_owner_timeline
    ON course_milestones(instructor_id, term, course_code, status, position);

ALTER TABLE milestone_submissions
    ADD COLUMN IF NOT EXISTS submitted_by_student_id BIGINT;

ALTER TABLE milestone_submissions
    ADD CONSTRAINT fk_milestone_submissions_submitter
        FOREIGN KEY (submitted_by_student_id) REFERENCES students(id) ON DELETE SET NULL;

-- Preserve grading context even if a timeline item's max score changes later.
ALTER TABLE milestone_grades
    ADD COLUMN IF NOT EXISTS max_score DECIMAL(5,2);

UPDATE milestone_grades mg
SET max_score = cm.max_score
FROM milestone_submissions ms
JOIN course_milestones cm ON cm.id = ms.milestone_id
WHERE mg.submission_id = ms.id
  AND mg.max_score IS NULL;

ALTER TABLE milestone_grades
    ALTER COLUMN max_score SET NOT NULL;
