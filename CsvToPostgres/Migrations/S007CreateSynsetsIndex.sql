
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'synset' AND indexname = 'idx_synset_synset') THEN
        CREATE INDEX idx_synset_synset ON synset USING GIN (synset);
    END IF;
END
$$;
