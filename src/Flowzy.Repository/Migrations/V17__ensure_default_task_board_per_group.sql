-- Ensure every group has exactly one default Kanban board.

-- If legacy/concurrent data contains multiple defaults, keep the oldest one.
WITH ranked_defaults AS (
    SELECT id,
           ROW_NUMBER() OVER (PARTITION BY group_id ORDER BY id) AS row_number
    FROM task_boards
    WHERE default_board = TRUE
)
UPDATE task_boards tb
SET default_board = FALSE,
    updated_at = CURRENT_TIMESTAMP
FROM ranked_defaults ranked
WHERE tb.id = ranked.id
  AND ranked.row_number > 1;

-- Backfill groups created after V12 that never received a default board.
INSERT INTO task_boards (
    group_id,
    name,
    description,
    position,
    default_board,
    created_at,
    updated_at
)
SELECT sg.id,
       sg.course_code,
       'Default Board',
       0,
       TRUE,
       CURRENT_TIMESTAMP,
       CURRENT_TIMESTAMP
FROM student_groups sg
WHERE NOT EXISTS (
    SELECT 1
    FROM task_boards tb
    WHERE tb.group_id = sg.id
      AND tb.default_board = TRUE
);

-- PostgreSQL partial unique index prevents two default boards for one group.
CREATE UNIQUE INDEX IF NOT EXISTS uq_task_boards_one_default_per_group
    ON task_boards(group_id)
    WHERE default_board = TRUE;
