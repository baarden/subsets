
with leadWords as (
	select p.id, w.word, w.wordidx,
		lead(w.word, 1) over (partition by p.id order by w.wordidx) leadWord
	from plusone p
		cross join unnest(words) with ordinality w(word, wordidx)
), badSets as (
	select distinct id
	from leadWords
	where word like '%s' and (wordidx = 1 or leadword like '%s')
)
update plusone p
set deleted = True
from badSets b
where p.id = b.id;

create index idx_plusone_deleted on plusone (deleted);

--
--

with leadWords as (
	select p.id, w.word, w.wordidx,
		lead(w.word, 1) over (partition by p.id order by w.wordidx) leadWord
	from plusonemore p
		cross join unnest(words) with ordinality w(word, wordidx)
), badSets as (
	select distinct id
	from leadWords
	where word like '%s' and (wordidx = 1 or leadword like '%s')
)
update plusonemore p
set deleted = True
from badSets b
where p.id = b.id;

create index idx_plusonemore_deleted on plusonemore (deleted);
