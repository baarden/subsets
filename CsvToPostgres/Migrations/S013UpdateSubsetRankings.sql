
create temporary table temp_rankings as
select d.id, sum(w.frequency) ranking
from plusone d
	cross join unnest(d.WordIds) di(id)
	join Words w on w.id = di.id
where d.deleted = false
group by d.id;

update plusone d
set ranking = tr.ranking
from temp_rankings tr
where d.id = tr.id;

--
--

drop table temp_rankings;

create temporary table temp_rankings as
select d.id, sum(w.frequency) ranking
from plusonemore d
	cross join unnest(d.WordIds) di(id)
	join Words w on w.id = di.id
where d.deleted = false
group by d.id;

update plusonemore d
set ranking = tr.ranking
from temp_rankings tr
where d.id = tr.id;
