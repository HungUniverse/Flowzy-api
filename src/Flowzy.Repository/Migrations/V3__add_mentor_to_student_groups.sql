ALTER TABLE student_groups ADD COLUMN mentor_id BIGINT;

ALTER TABLE student_groups
    ADD CONSTRAINT fk_student_groups_mentor
    FOREIGN KEY (mentor_id) REFERENCES mentors(id) ON DELETE SET NULL;

CREATE INDEX idx_student_groups_mentor_id ON student_groups(mentor_id);
