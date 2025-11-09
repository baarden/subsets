
CREATE INDEX idx_words_synsets ON words USING GIN (synsets gin__int_ops);
