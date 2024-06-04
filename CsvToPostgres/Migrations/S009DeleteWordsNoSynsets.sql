
update words ww
set synsets = sq.synsets
from (
	select w.id, coalesce(w1.synsets, w2.synsets, w3.synsets) synsets
	from words w
		left join words w1 on w.word = w1.word||'s' and w1.synsets is not null
		left join words w2 on (w.word = w2.word||'ed' or w.word = w2.word||'er') and w2.synsets is not null
		left join words w3 on (w.word = w3.word||'ing') and w3.synsets is not null
	where w.synsets is null
		and coalesce(w1.id, w2.id, w3.id) is not null
) sq
where ww.id = sq.id;

delete from words
where synsets is null;
