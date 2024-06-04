
create table Guess (Id SERIAL PRIMARY KEY, UserId INT, GuessDate DATE, GuessNumber INT, GuessWordIdx INT, Guess TEXT, Solved bool);
create index idx_guess_userid_guessdate on guess (userid, guessdate);
