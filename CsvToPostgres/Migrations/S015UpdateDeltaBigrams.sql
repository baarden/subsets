
CREATE INDEX idx_bigrams_bigram ON bigrams USING GIN (bigram);

create temporary table temp_bigrams (deltaid INT, bigrams INT[]);

with deltas as (
	select d.id, di.deltaId, row_number() over (partition by d.id) deltaseq, d.deltaids
	from deltas d
		cross join unnest(d.deltaIds) di(deltaId)
), leads as (
	select d.id, d.deltaids, d.deltaid, lead(d.deltaid, 1) over (partition by d.id order by d.deltaseq) deltaid2
	from deltas d
)
insert into temp_bigrams (deltaid, bigrams)
select l.id, array_agg(b.id)
from leads l
	join Bigrams b on b.bigram = ARRAY[l.deltaid, l.deltaid2]
where deltaid2 is not null
group by l.id, l.deltaids;

update Deltas d
set bigrams = tb.bigrams
from temp_bigrams tb
where tb.deltaid = d.id;
