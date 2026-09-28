ALTER TABLE mentor_meetings
    ADD COLUMN IF NOT EXISTS cancel_reason TEXT;
