-- Keep only the newest academic term open when upgrading legacy data.
CREATE TEMP TABLE fspark_auto_closed_terms (
    id BIGINT PRIMARY KEY
) ON COMMIT DROP;

INSERT INTO fspark_auto_closed_terms (id)
WITH ranked_open_terms AS (
    SELECT id,
           ROW_NUMBER() OVER (ORDER BY created_at DESC NULLS LAST, id DESC) AS open_rank
    FROM academic_terms
    WHERE status = 'OPEN'
)
SELECT id
FROM ranked_open_terms
WHERE open_rank > 1;

UPDATE academic_terms term
SET status = 'CLOSED',
    closed_at = COALESCE(term.closed_at, CURRENT_TIMESTAMP),
    updated_at = CURRENT_TIMESTAMP
FROM fspark_auto_closed_terms auto_closed
WHERE term.id = auto_closed.id;

-- Preserve the normal close-term feedback contract for terms closed by this migration.
INSERT INTO term_feedbacks (
    academic_term_id,
    group_id,
    student_id,
    target_type,
    mentor_id,
    instructor_id,
    status,
    version
)
SELECT term.id,
       group_row.id,
       member.student_id,
       'MENTOR',
       group_row.mentor_id,
       NULL,
       'PENDING',
       0
FROM academic_terms term
JOIN fspark_auto_closed_terms auto_closed ON auto_closed.id = term.id
JOIN student_groups group_row ON UPPER(group_row.term) = UPPER(term.code)
JOIN student_group_members member ON member.group_id = group_row.id
WHERE term.status = 'CLOSED'
  AND group_row.mentor_id IS NOT NULL
ON CONFLICT (academic_term_id, group_id, student_id, target_type) DO NOTHING;

INSERT INTO term_feedbacks (
    academic_term_id,
    group_id,
    student_id,
    target_type,
    mentor_id,
    instructor_id,
    status,
    version
)
SELECT term.id,
       group_row.id,
       member.student_id,
       'INSTRUCTOR',
       NULL,
       group_row.instructor_id,
       'PENDING',
       0
FROM academic_terms term
JOIN fspark_auto_closed_terms auto_closed ON auto_closed.id = term.id
JOIN student_groups group_row ON UPPER(group_row.term) = UPPER(term.code)
JOIN student_group_members member ON member.group_id = group_row.id
WHERE term.status = 'CLOSED'
  AND group_row.instructor_id IS NOT NULL
ON CONFLICT (academic_term_id, group_id, student_id, target_type) DO NOTHING;

-- Pending membership workflows cannot remain actionable after a term ends.
UPDATE group_invitations invitation
SET status = 'CANCELED',
    responded_at = COALESCE(invitation.responded_at, CURRENT_TIMESTAMP)
FROM student_groups group_row
JOIN academic_terms term ON UPPER(group_row.term) = UPPER(term.code)
WHERE invitation.group_id = group_row.id
  AND invitation.status = 'PENDING'
  AND term.status = 'CLOSED';

UPDATE group_join_requests request
SET status = 'CANCELED',
    responded_at = COALESCE(request.responded_at, CURRENT_TIMESTAMP),
    updated_at = CURRENT_TIMESTAMP
FROM student_groups group_row
JOIN academic_terms term ON UPPER(group_row.term) = UPPER(term.code)
WHERE request.group_id = group_row.id
  AND request.status = 'PENDING'
  AND term.status = 'CLOSED';

-- PostgreSQL partial uniqueness provides the final concurrency guarantee.
CREATE UNIQUE INDEX IF NOT EXISTS uq_academic_terms_single_open
    ON academic_terms (status)
    WHERE status = 'OPEN';
