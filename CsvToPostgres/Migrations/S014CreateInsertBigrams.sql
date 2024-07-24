
create table Bigrams (Id SERIAL PRIMARY KEY, Bigram INT[]);

with sets as (
	select d.id, di.wordId, row_number() over (partition by d.id) subsetseq
		from plusone d
			cross join unnest(d.wordIds) di(wordId)
		where d.deleted = false
	UNION ALL
	select d.id + 1000000, di.wordId, row_number() over (partition by d.id) subsetseq
		from plusonemore d
			cross join unnest(d.wordIds) di(wordId)
		where d.deleted = false
), leads as (
	select d.id, d.wordid, lead(d.wordid, 1) over (partition by d.id order by d.subsetseq) wordid2
	from sets d
)
insert into Bigrams (bigram)
select distinct ARRAY[l.wordid, l.wordid2]
from leads l
where l.wordid2 is not null;

create index ix_bigrams_bigram on bigrams USING GIN (bigram);
