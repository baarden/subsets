
CREATE INDEX idx_words_subsets ON words USING GIN (subsets);
CREATE INDEX idx_words_trigrams ON words USING GIN (trigrams);
CREATE INDEX idx_words_sorted ON words (Sorted);
CREATE INDEX idx_words_length ON words (length);

create table WordFrequencyStats (Id SERIAL PRIMARY KEY, Length INT, FreqMean FLOAT, FreqVariance FLOAT);

with stats as (
	select length, stats_agg(frequency) stats
	from Words
	group by length
)
insert into WordFrequencyStats (Length, FreqMean, FreqVariance)
select length, (s.stats).mean, (s.stats).variance
from stats s;

delete 
from Words w
	using WordFrequencyStats s
where s.length = w.length
	and w.frequency < (s.freqmean + sqrt(s.freqvariance) * 0.2513);
