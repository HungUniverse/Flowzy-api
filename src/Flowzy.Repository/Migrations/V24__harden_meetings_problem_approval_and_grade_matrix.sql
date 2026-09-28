-- Migration: V24__harden_meetings_problem_approval_and_grade_matrix.sql
-- Description: Allow contribution rows before group grading so instructor grading can lock contributions.

ALTER TABLE milestone_member_scores ALTER COLUMN group_grade_id DROP NOT NULL;
ALTER TABLE milestone_member_scores ALTER COLUMN calculated_score DROP NOT NULL;
ALTER TABLE milestone_member_scores ALTER COLUMN max_score_snapshot DROP NOT NULL;
ALTER TABLE milestone_member_scores ALTER COLUMN weight_snapshot DROP NOT NULL;
