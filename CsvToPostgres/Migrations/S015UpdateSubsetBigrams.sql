
CREATE INDEX idx_bigrams_bigram ON bigrams USING GIN (bigram);

create temporary table temp_bigrams (subsetid INT, bigrams INT[]);

with subsets as (
	select d.id, di.wordId, row_number() over (partition by d.id) subsetseq, d.wordids
	from subsets d
		cross join unnest(d.wordIds) di(wordId)
), leads as (
	select d.id, d.wordids, d.wordid, lead(d.wordid, 1) over (partition by d.id order by d.subsetseq) wordid2
	from subsets d
)
insert into temp_bigrams (subsetid, bigrams)
select l.id, array_agg(b.id)
from leads l
	join Bigrams b on b.bigram = ARRAY[l.wordid, l.wordid2]
where wordid2 is not null
group by l.id;

update subsets d
set bigrams = tb.bigrams
from temp_bigrams tb
where tb.subsetid = d.id;

create index ix_subsets_bigrams on subsets USING GIN (bigrams)