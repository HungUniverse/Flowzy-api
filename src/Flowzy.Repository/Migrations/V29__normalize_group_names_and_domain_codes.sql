-- Group names identify a group inside an academic term. Safely consolidate
-- legacy duplicate imports before enforcing the new unique index.
UPDATE student_groups
SET name = regexp_replace(btrim(name), '\\s+', ' ', 'g')
WHERE name <> regexp_replace(btrim(name), '\\s+', ' ', 'g');

CREATE TEMP TABLE fspark_group_merge_map ON COMMIT DROP AS
SELECT id AS duplicate_group_id,
       MIN(id) OVER (PARTITION BY lower(btrim(term)), lower(btrim(name))) AS canonical_group_id
FROM student_groups;

DELETE FROM fspark_group_merge_map WHERE duplicate_group_id = canonical_group_id;

-- Includes every legacy group participating in a merge and its final group ID.
CREATE TEMP TABLE fspark_group_merge_scope ON COMMIT DROP AS
SELECT duplicate_group_id AS source_group_id,
       canonical_group_id AS target_group_id,
       FALSE AS is_canonical
FROM fspark_group_merge_map
UNION
SELECT DISTINCT canonical_group_id AS source_group_id,
       canonical_group_id AS target_group_id,
       TRUE AS is_canonical
FROM fspark_group_merge_map;

DO $$
BEGIN
    -- The new business rule identifies a group by term + normalized name only.
    -- When legacy duplicates have different course codes, retain the course code
    -- from the canonical (lowest ID) group and merge the remaining relationships.
    IF EXISTS (
        SELECT 1
        FROM milestone_submissions item
        JOIN fspark_group_merge_scope scope ON scope.source_group_id = item.group_id
        GROUP BY scope.target_group_id, item.milestone_id
        HAVING COUNT(*) > 1
    ) OR EXISTS (
        SELECT 1
        FROM milestone_group_grades item
        JOIN fspark_group_merge_scope scope ON scope.source_group_id = item.group_id
        GROUP BY scope.target_group_id, item.milestone_id
        HAVING COUNT(*) > 1
    ) OR EXISTS (
        SELECT 1
        FROM milestone_member_scores item
        JOIN fspark_group_merge_scope scope ON scope.source_group_id = item.group_id
        GROUP BY scope.target_group_id, item.milestone_id, item.student_id
        HAVING COUNT(*) > 1
    ) OR EXISTS (
        SELECT 1
        FROM term_feedbacks item
        JOIN fspark_group_merge_scope scope ON scope.source_group_id = item.group_id
        GROUP BY scope.target_group_id, item.academic_term_id, item.student_id, item.target_type
        HAVING COUNT(*) > 1
    ) OR EXISTS (
        SELECT 1
        FROM group_recruitment_needs item
        JOIN fspark_group_merge_scope scope ON scope.source_group_id = item.group_id
        GROUP BY scope.target_group_id, item.role
        HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'Cannot merge duplicate groups with conflicting graded, submission, feedback, or recruitment data. Resolve the records before applying V29.';
    END IF;
END $$;

-- A student may belong to multiple legacy duplicates. Keep one membership
-- before moving them to the final group, so its unique key remains valid.
WITH ranked_members AS (
    SELECT member.id,
           ROW_NUMBER() OVER (
               PARTITION BY scope.target_group_id, member.student_id
               ORDER BY scope.is_canonical DESC, member.id ASC
           ) AS row_number
    FROM student_group_members member
    JOIN fspark_group_merge_scope scope ON scope.source_group_id = member.group_id
)
DELETE FROM student_group_members member
USING ranked_members ranked
WHERE member.id = ranked.id
  AND ranked.row_number > 1;

UPDATE student_group_members child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;

-- Only one default board can exist after a group merge. Prefer the canonical
-- group's board, then retain the oldest duplicate board if needed.
WITH ranked_default_boards AS (
    SELECT board.id,
           ROW_NUMBER() OVER (
               PARTITION BY scope.target_group_id
               ORDER BY scope.is_canonical DESC, board.id ASC
           ) AS row_number
    FROM task_boards board
    JOIN fspark_group_merge_scope scope ON scope.source_group_id = board.group_id
    WHERE board.default_board = TRUE
)
UPDATE task_boards board
SET default_board = FALSE,
    updated_at = CURRENT_TIMESTAMP
FROM ranked_default_boards ranked
WHERE board.id = ranked.id
  AND ranked.row_number > 1;

-- Pending requests/invitations for an equivalent group are deduplicated
-- before their group IDs are moved, avoiding partial unique-index conflicts.
WITH ranked_pending_requests AS (
    SELECT request.id,
           ROW_NUMBER() OVER (
               PARTITION BY scope.target_group_id, request.student_id
               ORDER BY scope.is_canonical DESC, request.created_at ASC, request.id ASC
           ) AS row_number
    FROM group_join_requests request
    JOIN fspark_group_merge_scope scope ON scope.source_group_id = request.group_id
    WHERE request.status = 'PENDING'
)
UPDATE group_join_requests request
SET status = 'CANCELED',
    updated_at = CURRENT_TIMESTAMP
FROM ranked_pending_requests ranked
WHERE request.id = ranked.id
  AND ranked.row_number > 1;

WITH ranked_pending_invitations AS (
    SELECT invitation.id,
           ROW_NUMBER() OVER (
               PARTITION BY scope.target_group_id, invitation.invitee_student_id
               ORDER BY scope.is_canonical DESC, invitation.created_at ASC, invitation.id ASC
           ) AS row_number
    FROM group_invitations invitation
    JOIN fspark_group_merge_scope scope ON scope.source_group_id = invitation.group_id
    WHERE invitation.status = 'PENDING'
)
UPDATE group_invitations invitation
SET status = 'CANCELED',
    responded_at = CURRENT_TIMESTAMP
FROM ranked_pending_invitations ranked
WHERE invitation.id = ranked.id
  AND ranked.row_number > 1;

UPDATE task_boards child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE group_tasks child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE task_activities child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE group_join_requests child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE group_invitations child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE mentor_meetings child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE milestone_submissions child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE term_feedbacks child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE group_recruitment_needs child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE milestone_group_grades child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE milestone_member_scores child SET group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.group_id = m.duplicate_group_id;
UPDATE problems child SET proposed_by_group_id = m.canonical_group_id FROM fspark_group_merge_map m WHERE child.proposed_by_group_id = m.duplicate_group_id;

-- Preserve assignments from a duplicate group. If several duplicates have a
-- value for the same assignment, the most recently updated one wins.
UPDATE student_groups canonical_group
SET mentor_id = COALESCE((
        SELECT duplicate_group.mentor_id
        FROM fspark_group_merge_map m
        JOIN student_groups duplicate_group ON duplicate_group.id = m.duplicate_group_id
        WHERE m.canonical_group_id = canonical_group.id
          AND duplicate_group.mentor_id IS NOT NULL
        ORDER BY duplicate_group.updated_at DESC NULLS LAST, duplicate_group.id DESC
        LIMIT 1
    ), canonical_group.mentor_id),
    instructor_id = COALESCE((
        SELECT duplicate_group.instructor_id
        FROM fspark_group_merge_map m
        JOIN student_groups duplicate_group ON duplicate_group.id = m.duplicate_group_id
        WHERE m.canonical_group_id = canonical_group.id
          AND duplicate_group.instructor_id IS NOT NULL
        ORDER BY duplicate_group.updated_at DESC NULLS LAST, duplicate_group.id DESC
        LIMIT 1
    ), canonical_group.instructor_id),
    selected_problem_id = COALESCE((
        SELECT duplicate_group.selected_problem_id
        FROM fspark_group_merge_map m
        JOIN student_groups duplicate_group ON duplicate_group.id = m.duplicate_group_id
        WHERE m.canonical_group_id = canonical_group.id
          AND duplicate_group.selected_problem_id IS NOT NULL
        ORDER BY duplicate_group.updated_at DESC NULLS LAST, duplicate_group.id DESC
        LIMIT 1
    ), canonical_group.selected_problem_id)
WHERE EXISTS (
    SELECT 1
    FROM fspark_group_merge_map m
    WHERE m.canonical_group_id = canonical_group.id
);

DELETE FROM student_groups duplicate_group
USING fspark_group_merge_map m
WHERE duplicate_group.id = m.duplicate_group_id;

ALTER TABLE student_groups DROP CONSTRAINT IF EXISTS uq_student_groups_term_course_group_no;
CREATE UNIQUE INDEX IF NOT EXISTS uq_student_groups_term_normalized_name
    ON student_groups (lower(btrim(term)), lower(btrim(name)));

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM problem_domains
        GROUP BY lower(btrim(code))
        HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'Cannot normalize problem domain codes because case-insensitive duplicates exist.';
    END IF;
END $$;

UPDATE problem_domains SET code = upper(btrim(code));
CREATE UNIQUE INDEX IF NOT EXISTS uq_problem_domains_normalized_code
    ON problem_domains (lower(btrim(code)));
