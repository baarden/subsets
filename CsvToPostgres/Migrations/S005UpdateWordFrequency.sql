
CREATE INDEX idx_words_subsets ON words USING GIN (subsets);
CREATE INDEX idx_words_trigrams ON words USING GIN (trigrams);
CREATE INDEX idx_words_sorted ON words (Sorted);
CREATE INDEX idx_words_length ON words (length);
create index idx_words_word on words (word);

create index idx_wordfreq_word on wordfreq (word);

update words w
set frequency = wf.frequency
from wordfreq wf
where wf.word = w.word
;

create index idx_words_frequency on words (frequency);
