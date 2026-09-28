-- Migration: V14__add_resubmitted_and_max_score.sql
-- Description: Add max_score to course_milestones and allow RESUBMITTED status in milestone_submissions

ALTER TABLE course_milestones ADD COLUMN max_score DECIMAL(5,2) DEFAULT 10.00 NOT NULL;

ALTER TABLE milestone_submissions DROP CONSTRAINT IF EXISTS chk_milestone_submissions_status;
ALTER TABLE milestone_submissions ADD CONSTRAINT chk_milestone_submissions_status CHECK (status IN ('SUBMITTED', 'RESUBMITTED', 'GRADED'));
