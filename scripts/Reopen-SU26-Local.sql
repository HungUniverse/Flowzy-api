-- One-off recovery explicitly requested by the local operator.
-- Preserve feedback, memberships, account states and all business data.
-- Account/group unarchive requires separate authorization.
\set ON_ERROR_STOP on
BEGIN;
SET LOCAL lock_timeout = '10s';
LOCK TABLE academic_terms, student_groups, course_milestones, term_feedbacks IN SHARE ROW EXCLUSIVE MODE;
DO $recovery$
DECLARE changed integer;
BEGIN
  IF current_database() <> 'flowzy' THEN RAISE EXCEPTION 'Unexpected database'; END IF;
  IF NOT EXISTS (SELECT 1 FROM academic_terms WHERE id=1 AND code='SU26' AND status='CLOSED')
    THEN RAISE EXCEPTION 'SU26 is not the expected closed term'; END IF;
  IF NOT EXISTS (SELECT 1 FROM academic_terms WHERE id=2 AND code='FA26' AND status='OPEN')
    THEN RAISE EXCEPTION 'FA26 is not the expected open term'; END IF;
  IF EXISTS (SELECT 1 FROM academic_terms WHERE status='OPEN' AND code <> 'FA26')
    THEN RAISE EXCEPTION 'Another open term exists'; END IF;
  IF EXISTS (SELECT 1 FROM student_groups WHERE upper(term)='FA26')
     OR EXISTS (SELECT 1 FROM course_milestones WHERE upper(term)='FA26')
     OR EXISTS (SELECT 1 FROM term_feedbacks WHERE academic_term_id=2)
    THEN RAISE EXCEPTION 'FA26 has dependent data; refuse deletion'; END IF;
  DELETE FROM academic_terms WHERE id=2 AND code='FA26';
  GET DIAGNOSTICS changed = ROW_COUNT;
  IF changed <> 1 THEN RAISE EXCEPTION 'Unexpected delete count'; END IF;
  UPDATE academic_terms SET status='OPEN', closed_at=NULL, closed_by_account_id=NULL, updated_at=now()
    WHERE id=1 AND code='SU26' AND status='CLOSED';
  GET DIAGNOSTICS changed = ROW_COUNT;
  IF changed <> 1 THEN RAISE EXCEPTION 'Unexpected update count'; END IF;
END $recovery$;
COMMIT;
SELECT id,code,status,closed_at,closed_by_account_id FROM academic_terms ORDER BY id;
SELECT status,count(*) AS groups FROM student_groups WHERE term='SU26' GROUP BY status;
SELECT status,count(*) AS feedbacks FROM term_feedbacks WHERE academic_term_id=1 GROUP BY status;
