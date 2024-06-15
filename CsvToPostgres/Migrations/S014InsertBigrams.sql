
with sets as (
	select d.id, di.wordId, row_number() over (partition by d.id) subsetseq
	from subsets d
		cross join unnest(d.wordIds) di(wordId)
), leads as (
	select d.id, d.wordid, lead(d.wordid, 1) over (partition by d.id order by d.subsetseq) wordid2
	from sets d
)
insert into Bigrams (bigram)
select distinct ARRAY[l.wordid, l.wordid2]
from leads l
where l.wordid2 is not null;

create index ix_bigrams_bigram on bigrams USING GIN (bigram);
