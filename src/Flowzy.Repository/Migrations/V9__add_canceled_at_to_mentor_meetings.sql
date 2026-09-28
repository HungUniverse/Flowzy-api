ALTER TABLE mentor_meetings
    ADD COLUMN IF NOT EXISTS canceled_at TIMESTAMPTZ;
