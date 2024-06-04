
create table if not exists NonWords (Id SERIAL PRIMARY KEY, NonWord TEXT);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'nonwords' AND indexname = 'idx_nonwords_nonword') THEN
        create index idx_nonwords_nonword on nonwords (nonword);
    END IF;
END
$$;

