
CREATE TABLE Words (
	Id SERIAL PRIMARY KEY,
    Word TEXT,
    Length INT,
    Frequency FLOAT,
    Sorted TEXT,
    Subsets TEXT[],
	Trigrams TEXT[],
	Chars CHAR[],
	SynSets INT[]
   );
