
create temporary table temp_bigrams (subsetid INT, bigrams INT[]);

with subsetWords as (
	select d.id, di.wordId, row_number() over (partition by d.id) subsetseq, d.wordids
	from plusone d
		cross join unnest(d.wordIds) di(wordId)
	where deleted = false
), leads as (
	select d.id, d.wordids, d.wordid, lead(d.wordid, 1) over (partition by d.id order by d.subsetseq) wordid2
	from subsetWords d
)
insert into temp_bigrams (subsetid, bigrams)
select l.id, array_agg(b.id)
from leads l
	join Bigrams b on b.bigram = ARRAY[l.wordid, l.wordid2]
where wordid2 is not null
group by l.id;

update plusone d
set bigrams = tb.bigrams
from temp_bigrams tb
where tb.subsetid = d.id;

create index ix_plusone_bigrams on plusone USING GIN (bigrams);

--
--

truncate table temp_bigrams;

with subsetWords as (
	select d.id, di.wordId, row_number() over (partition by d.id) subsetseq, d.wordids
	from plusonemore d
		cross join unnest(d.wordIds) di(wordId)
	where deleted = false
), leads as (
	select d.id, d.wordids, d.wordid, lead(d.wordid, 1) over (partition by d.id order by d.subsetseq) wordid2
	from subsetWords d
)
insert into temp_bigrams (subsetid, bigrams)
select l.id, array_agg(b.id)
from leads l
	join Bigrams b on b.bigram = ARRAY[l.wordid, l.wordid2]
where wordid2 is not null
group by l.id;

update plusonemore d
set bigrams = tb.bigrams
from temp_bigrams tb
where tb.subsetid = d.id;

create index ix_plusonemore_bigrams on plusonemore USING GIN (bigrams);
