
create temporary table temp_rankings as
select d.id, sum(w.frequency) ranking
from subsets d
	cross join unnest(d.WordIds) di(id)
	join Words w on w.id = di.id
group by d.id;

update subsets d
set ranking = tr.ranking
from temp_rankings tr
where d.id = tr.id;
