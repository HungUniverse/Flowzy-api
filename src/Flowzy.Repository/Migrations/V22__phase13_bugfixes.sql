-- Migration: V22__phase13_bugfixes.sql
-- Description: Normalize default task board display names for Phase 13.

UPDATE task_boards
SET name = 'Default',
    updated_at = CURRENT_TIMESTAMP
WHERE default_board = TRUE
  AND name <> 'Default';
