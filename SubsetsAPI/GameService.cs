using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.OpenApi.Services;
using Npgsql;
using SubsetsAPI.Models;
using static SubsetsAPI.Constants;

namespace SubsetsAPI;

public class GameService
{
    private readonly string _connectionString;

    public GameService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public int? GetUser(string sessionId)
    {
        int? userId;

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT Id FROM Users WHERE SessionId = @SessionId", conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        userId = (int?)cmd.ExecuteScalar() ?? CreateUser(sessionId, conn);

        return userId;
    }

    private static int? CreateUser(string sessionId, NpgsqlConnection conn)
    {
        int? userId;

        string insertQuery = @"INSERT INTO Users (SessionId) VALUES (@SessionId) RETURNING Id";
        using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        userId = (int?)cmd.ExecuteScalar();

        return userId;
    }

    public (Status, string?) GetStatus(int userId)
    {
        DateOnly today = GetDate();
        var status = new Status
        {
            Today = today
        };

        var gameData = new GameDayDataProvider(today, _connectionString);
        status.ClueWord = gameData.ClueWord;

        int key = 0;
        AddStartingGuess(status, gameData);

        List<GuessData> guessDataList = FetchGuessData(userId, today);
        int maxWordIndex = 1;
        foreach (var guessData in guessDataList)
        {
            int offset = 0;
            string highlightLetter = gameData.HighlightLetter(guessData.ReferenceWordIndex);
            Guess guess = GetGuess(
                ++key,
                guessData.GuessText,
                gameData.ReferenceWord(guessData.ReferenceWordIndex), 
                guessData.ReferenceWordIndex, 
                offset,
                highlightLetter
            );
            status.Guesses.Add(guess);

            maxWordIndex = (guess.State == GuessState.Solved) ? guess.WordIndex + 1 : guess.WordIndex;

            if (guess.State == GuessState.Solved && maxWordIndex == ExtraLetterIndex) {
                string finalLetter = gameData.HighlightLetter(ExtraLetterIndex);
                Guess finalGuess = GetGuess(++key, finalLetter, finalLetter, ExtraLetterIndex, 1, finalLetter);
                status.Guesses.Add(finalGuess);
                maxWordIndex++;
            }
        }

        status.State = (maxWordIndex <= AnagramIndex) ? GameState.Unsolved : GameState.Solved;
        string? refWord = null;

        int refCharIdx = (maxWordIndex == AnagramIndex) ? AnagramIndex : LastPlusOneIndex;
        List<char> gameChars = gameData.ReferenceWord(refCharIdx).ToList(); 
        string prevWord = (maxWordIndex == AnagramIndex) ? "" : gameData.ReferenceWord(maxWordIndex - 1);
        gameChars.Sort((a, b) =>
        {
            bool aInRef = prevWord.Contains(a);
            bool bInRef = prevWord.Contains(b);
            if (aInRef && !bInRef) return -1;
            if (!aInRef && bInRef) return 1;
            return a.CompareTo(b);
        });
        status.Characters = gameChars;

        if (status.State == GameState.Unsolved)
        {
            refWord = gameData.ReferenceWord(maxWordIndex);
            status.NextGuess = GetGuess(++key, "", refWord, maxWordIndex, 0);
        } else {
            int[] newOrder = gameData.AnagramSortOrder();
            List<Guess> guesses = newOrder.SelectMany(i => status.Guesses.Where(g => g.WordIndex == i)).ToList();
            status.Guesses = guesses;
        }
 
        status.Indent = 0;

        return (status, refWord);
    }

    public Statistics GetStatistics(int userId)
    {
        DateOnly today = GetDate();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string guessQuery = @"
            with guessdata as (
                select guessdate, cast(solved as int) solved, 1 played
                    from guess
                    where userid = @userId
                union all
                select @today, 0, 0
                union all
                select '2023-01-01', 0, 1
            ), dates as (
                select guessdate, max(solved) solved, max(played) played
                from guessdata
                group by guessdate
            ), dateLag as (
                select *, d.guessDate - lag(d.guessDate, 1) over (order by d.guessdate) dateLag
                from dates d
                where d.solved = 1
            ), streak as (
                select coalesce((@today - min(guessDate)) + 1, 0) streakLen
                from dateLag dl
                where dl.dateLag > 1
            )
            select count(*) played, sum(d.solved) solved, s.streakLen streakLen
            from dates d
                cross join streak s
            where d.played = 1
            group by s.streakLen;
        ";

        using var cmd = new NpgsqlCommand(guessQuery, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@today", today);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int played = Convert.ToInt32(reader["played"]);
            int solved = Convert.ToInt32(reader["solved"]);
            int streak = Convert.ToInt32(reader["streakLen"]);

            var stats = new Statistics {
                Played = played,
                Solved = solved,
                Streak = streak
            };
            return stats;
        }

        throw new Exception("Unable to read statistics!");
    }

    private static DateOnly GetDate()
    {
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        return DateOnly.FromDateTime(now);
    }

    private static void AddStartingGuess(Status status, GameDayDataProvider gameData)
    {
        int index = 0;
        string startWord = gameData.ReferenceWord(index);
        string highlightLetter = gameData.HighlightLetter(index);
        status.Guesses.Add(GetGuess(0, startWord, startWord, index, 0, highlightLetter));
    }

    private List<GuessData> FetchGuessData(int userId, DateOnly playDate)
    {
        var guesses = new List<GuessData>();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string guessQuery = @"
            SELECT Guess, GuessWordIdx, GuessNumber
            FROM Guess
            WHERE UserId = @UserId AND GuessDate = @PlayDate
            ORDER BY GuessNumber;
        ";

        using var cmd = new NpgsqlCommand(guessQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@PlayDate", playDate);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string guessText = (string)reader["Guess"];
            int referenceWordIndex = (int)reader["GuessWordIdx"];
            int guessNumber = (int)reader["GuessNumber"];

            guesses.Add(new GuessData(GuessText: guessText, ReferenceWordIndex: referenceWordIndex, GuessNumber: guessNumber));
        }

        return guesses;
    }

    private static Guess GetGuess(
        int key,
        string guess,
        string referenceWord,
        int wordIndex,
        int offset,
        string highlightLetter="")
    {
        var newGuess = new Guess
        {
            Key = key,
            GuessWord = guess,
            WordIndex = wordIndex,
            Length = referenceWord.Length,
            Offset = offset,
            Characters = GetClues(guess, referenceWord),
            State = GuessState.Unsolved
        };

        if (newGuess.Characters.All(c => c.Type == ClueType.AllCorrect))
        {
            newGuess.State = GuessState.Solved;
            newGuess.HighlightLetter = highlightLetter;
        }
        return newGuess;
    }

    private static List<Clue> GetClues(string guess, string referenceWord)
    {
        var clues = new List<Clue>();
        var referenceCharCount = new Dictionary<char, int>();
        var correctLetterCount = new Dictionary<char, int>();

        // Initialize counts from the reference word
        foreach (char c in referenceWord)
        {
            if (referenceCharCount.TryGetValue(c, out int value))
                referenceCharCount[c] = ++value;
            else
                referenceCharCount[c] = 1;

            correctLetterCount[c] = 0;
        }

        // First pass: Identify AllCorrect clues and adjust correctLetterCount
        for (int i = 0; i < referenceWord.Length; i++)
        {
            if (i < guess.Length && guess[i] == referenceWord[i])
            {
                correctLetterCount[guess[i]]++;
            }
        }

        // Second pass: Generate clues using adjusted counts
        for (int i = 0; i < referenceWord.Length; i++)
        {
            char refChar = referenceWord[i];
            char guessChar = i < guess.Length ? guess[i] : ' ';
            ClueType type;

            if (guessChar == ' ')
            {
                type = ClueType.Empty;
            }
            else if (guessChar == refChar)
            {
                type = ClueType.AllCorrect;
            }
            else if (referenceWord.Contains(guessChar))
            {
                if (correctLetterCount[guessChar] < referenceCharCount[guessChar])
                {
                    type = ClueType.CorrectLetter;
                    correctLetterCount[guessChar]++;
                }
                else
                {
                    type = ClueType.Incorrect;
                }
            }
            else
            {
                type = ClueType.Incorrect;
            }

            clues.Add(new Clue { Letter = guessChar, Type = type });
        }

        return clues;
    }

    public bool AddGuess(int userId, DateOnly guessDate, int guessNumber, int guessWordIndex, string guess, out string errorMessage)
    {
        errorMessage = string.Empty;
        string trimGuess = guess.Trim();

        var gameData = new GameDayDataProvider(guessDate, _connectionString);

        string refWord = gameData.ReferenceWord(guessWordIndex);

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string checkGuessQuery = @"
            SELECT COUNT(*)
            FROM Words w
            WHERE w.Word = @Guess;
        ";

        using var checkCmd = new NpgsqlCommand(checkGuessQuery, conn);
        checkCmd.Parameters.AddWithValue("@Guess", trimGuess);

        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (count == 0)
        {
            errorMessage = "Not in the word list";
            return false;
        }

        Guess guessData = GetGuess(0, guess, refWord, guessWordIndex, 0);
        bool solved = guessData.Characters.All(c => c.Type == ClueType.AllCorrect);

        string insertQuery = @"
            INSERT INTO Guess (UserId, GuessDate, GuessNumber, GuessWordIdx, Guess, Solved)
            VALUES (@UserId, @GuessDate, @GuessNumber, @GuessWordIndex, @Guess, @Solved);
        ";

        using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@GuessDate", guessDate);
        cmd.Parameters.AddWithValue("@GuessNumber", guessNumber);
        cmd.Parameters.AddWithValue("@GuessWordIndex", guessWordIndex);
        cmd.Parameters.AddWithValue("@Guess", guess);
        cmd.Parameters.AddWithValue("@Solved", solved);

        int result = cmd.ExecuteNonQuery();
        return result == 1;
    }

}

public class GameDayDataProvider
{
    public readonly string ClueWord;
    private readonly string _connectionString;
    private readonly GameDayData _gameDayData;

    public GameDayDataProvider(DateOnly playDate, string connectionString)
    {
        _connectionString = connectionString;
        _gameDayData = GetGameDayData(playDate);
        ClueWord = _gameDayData.ClueWord;
    }

    public string ReferenceWord(int index)
    {
        return (index == AnagramIndex) ? _gameDayData.Anagram : _gameDayData.SubsetWords[index];
    }

    public int Offset(int index)
    {
        return 1;
    }

    public string HighlightLetter(int wordIndex)
    {
        if (wordIndex == AnagramIndex) { return ""; }
        return _gameDayData.AnagramSources[wordIndex];
    }

    public int[] AnagramSortOrder()
    {
        string[] words = _gameDayData.SubsetWords;
        string anagram = _gameDayData.Anagram;
        List<string> charSources = _gameDayData.AnagramSources.ToList();

        var sortOrder = new int[anagram.Length];
        for (int i = 0; i < anagram.Length; i++)
        {
            int idx = charSources.IndexOf(anagram[i].ToString());
            sortOrder[i] = idx;
            charSources[idx] = "";
        }
        return sortOrder;
    }

    private GameDayData GetGameDayData(DateOnly playDate)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string deltaQuery = @"
            SELECT ClueWord, Anagram, AnagramSources, Words
            FROM subsets2
            WHERE PlayDate = @PlayDate;
        ";

        using var cmd = new NpgsqlCommand(deltaQuery, conn);
        cmd.Parameters.AddWithValue("@PlayDate", playDate);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var clueWord = (string)reader["ClueWord"];
            var anagram = (string)reader["Anagram"];
            var anagramSourceArr = (string[])reader["AnagramSources"];
            var words = (string[])reader["Words"];

            List<string> anagramSourceList = anagramSourceArr.ToList();
            anagramSourceList.Reverse();
            string extra = anagramSourceList[0];
            anagramSourceList.RemoveAt(0);
            anagramSourceList.Add(extra);
            string[] anagramSources = anagramSourceList.ToArray();

            Array.Reverse(words);

            return new GameDayData(ClueWord: clueWord, Anagram: anagram, AnagramSources: anagramSources, SubsetWords: words);
        }

        throw new Exception("No game data today");
    }
}