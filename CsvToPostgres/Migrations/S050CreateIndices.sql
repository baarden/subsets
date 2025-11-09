
create index ix_plusone_batchpool on plusone (batchpool);
create index ix_plusone_wordids on plusone USING GIN (wordids gin__int_ops);
create index ix_plusone_anagram on plusone (anagram);
create index ix_plusone_playdate on plusone (playdate);

create index ix_plusonemore_batchpool on plusonemore (batchpool);
create index ix_plusonemore_wordids on plusonemore USING GIN (wordids gin__int_ops);
create index ix_plusonemore_anagram on plusonemore (anagram);
create index ix_plusonemore_playdate on plusonemore (playdate);
