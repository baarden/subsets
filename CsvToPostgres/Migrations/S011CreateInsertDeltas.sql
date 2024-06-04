
CREATE TABLE deltas (
	Id SERIAL PRIMARY KEY,
	deltaWords TEXT[],
	DeltaIds INT[],
	Bigrams INT[],
	Anagram TEXT,
	AnagramOffsets INT[],
	ClueWord TEXT,
	Ranking FLOAT,
	Batch INT,
	PlayDate DATE);

insert into deltas (deltaWords, DeltaIds)
select ARRAY[w3.word, w4.word, w5.word, w6.word, w7.word, w8.word] deltaWords,
	ARRAY[w3.id, w4.id, w5.id, w6.id, w7.id, w8.id] WordIds
from words w8
	join words w7 on w7.sorted = any(w8.subsets) and not (w7.trigrams && w8.trigrams) 
	join words w6 on w6.sorted = any(w7.subsets) and not (w6.trigrams && w7.trigrams) 
	join words w5 on w5.sorted = any(w6.subsets) and not (w5.trigrams && w6.trigrams) 
	join words w4 on w4.sorted = any(w5.subsets) and not (w4.trigrams && w5.trigrams) 
	join words w3 on w3.sorted = any(w4.subsets) and not (w3.trigrams && w4.trigrams)
where w8.length = 8;
