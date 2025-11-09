
create table if not exists BadWordPairs (Id SERIAL PRIMARY KEY, WordPair INT[]);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'badwordpairs' AND indexname = 'idx_badwordpairs_wordpair') THEN
        create index idx_badwordpairs_wordpair on BadWordPairs USING GIN (WordPair gin__int_ops);
    END IF;
END
$$;

