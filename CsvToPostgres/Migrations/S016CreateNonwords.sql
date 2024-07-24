
create table NonWordPlusOne (Id SERIAL PRIMARY KEY, NonWord TEXT);
create index idx_nonwordplusone_nonword on nonwordplusone (nonword);

create table NonWordPlusOneMore (Id SERIAL PRIMARY KEY, NonWord TEXT);
create index idx_nonwordplusonemore_nonword on nonwordplusonemore (nonword);
