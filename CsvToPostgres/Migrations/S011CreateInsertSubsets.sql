
CREATE TABLE subsets (
	Id SERIAL PRIMARY KEY,
	Words TEXT[],
	WordIds INT[],
	Bigrams INT[],
	Anagram TEXT,
	AnagramOffsets INT[],
	ClueWord TEXT,
	Ranking FLOAT,
	Batch INT,
	BatchPool INT,
	Processed BOOL
	PlayDate DATE);

insert into subsets (Words, WordIds, BatchPool)
select ARRAY[w8.word, w7.word, w6.word, w5.word, w4.word, w3.word],
	ARRAY[w8.id, w7.id, w6.id, w5.id, w4.id, w3.id] WordIds,
	1
from words w8
	join words w7 on w7.sorted = any(w8.subsets) and not (w7.trigrams && w8.trigrams) and w7.frequency > 13
	join words w6 on w6.sorted = any(w7.subsets) and not (w6.trigrams && w7.trigrams) and w6.frequency > 13
	join words w5 on w5.sorted = any(w6.subsets) and not (w5.trigrams && w6.trigrams) and w5.frequency > 13
	join words w4 on w4.sorted = any(w5.subsets) and not (w4.trigrams && w5.trigrams) and w4.frequency > 13
	join words w3 on w3.sorted = any(w4.subsets) and not (w3.trigrams && w4.trigrams) and w3.frequency > 13
where w8.length = 8 and w8.frequency > 13
;
