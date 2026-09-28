-- One-off local unarchive explicitly authorized by the operator.
-- Only SU26 groups and their jointly inactive STUDENT account/profile pairs.
\set ON_ERROR_STOP on
BEGIN;
SET LOCAL lock_timeout = '10s';
LOCK TABLE academic_terms,student_groups,student_group_members,students,accounts IN SHARE ROW EXCLUSIVE MODE;
DO $guard$
BEGIN
  IF current_database()<>'flowzy' OR NOT EXISTS(SELECT 1 FROM academic_terms WHERE id=1 AND code='SU26' AND status='OPEN')
    THEN RAISE EXCEPTION 'Expected open local SU26'; END IF;
  IF EXISTS(SELECT 1 FROM student_groups WHERE term='SU26' AND status NOT IN ('ACTIVE','INACTIVE'))
    THEN RAISE EXCEPTION 'Unexpected group state'; END IF;
END $guard$;
CREATE TEMP TABLE su26_reactivation_targets ON COMMIT DROP AS
SELECT DISTINCT s.id AS student_id,a.id AS account_id
FROM students s JOIN accounts a ON a.id=s.account_id
JOIN student_group_members m ON m.student_id=s.id
JOIN student_groups g ON g.id=m.group_id
WHERE g.term='SU26' AND a.role='STUDENT' AND s.status='INACTIVE' AND a.status='INACTIVE';
UPDATE accounts a SET status='ACTIVE',updated_at=now()
FROM su26_reactivation_targets t WHERE a.id=t.account_id;
UPDATE students s SET status='ACTIVE',updated_at=now()
FROM su26_reactivation_targets t WHERE s.id=t.student_id;
UPDATE student_groups SET status='ACTIVE',updated_at=now() WHERE term='SU26' AND status='INACTIVE';
SELECT count(*) AS reactivated_student_accounts FROM su26_reactivation_targets;
COMMIT;
SELECT code,status FROM academic_terms;
SELECT status,count(*) AS groups FROM student_groups WHERE term='SU26' GROUP BY status;
SELECT a.role,a.status,count(*) FROM accounts a GROUP BY a.role,a.status ORDER BY a.role,a.status;
