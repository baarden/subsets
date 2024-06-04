
with deltas as (
	select d.id, di.deltaId, row_number() over (partition by d.id) deltaseq, d.deltaids
	from deltas d
		cross join unnest(d.deltaIds) di(deltaId)
), leads as (
	select d.id, d.deltaids, d.deltaid, lead(d.deltaid, 1) over (partition by d.id order by d.deltaseq) deltaid2
	from deltas d
)
insert into Bigrams (bigram)
select distinct ARRAY[l.deltaid, l.deltaid2]
from leads l
where l.deltaid2 is not null;
