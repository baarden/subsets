

create table Users (Id SERIAL PRIMARY KEY, SessionId TEXT);
create index ix_users_sessionid on users (sessionid);
