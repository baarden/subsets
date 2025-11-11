
create table Guess (
    Id SERIAL PRIMARY KEY,
    UserId INT,
    IsMore BOOLEAN,
    GuessDate DATE,
    GuessNumber INT,
    GuessWordIdx INT,
    Guess TEXT,
    HighlightIdx INT,
    Solved bool
    );
create index ix_guess_userid_guessdate_guess on guess (userid, guessdate, trim(guess));
