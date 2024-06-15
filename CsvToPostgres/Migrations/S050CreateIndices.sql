
create index ix_subsets_batchpool on subsets (batchpool);
create index ix_subsets_wordids on subsets USING GIN (wordids);
create index ix_subsets_anagram on subsets (anagram);
create index ix_subsets_playdate on subsets (playdate);
