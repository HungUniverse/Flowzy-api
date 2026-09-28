-- Migration: V15__allow_null_milestone_weight.sql
-- Description: Allow null weight and add INACTIVE status for course milestones to support adversarial test cases

ALTER TABLE course_milestones ALTER COLUMN weight DROP NOT NULL;

ALTER TABLE course_milestones DROP CONSTRAINT IF EXISTS chk_course_milestones_status;
ALTER TABLE course_milestones ADD CONSTRAINT chk_course_milestones_status CHECK (status IN ('ACTIVE', 'ARCHIVED', 'INACTIVE'));
