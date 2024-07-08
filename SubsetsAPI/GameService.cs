using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Services;
using Npgsql;
using SubsetsAPI.Models.ValueObjects;

namespace SubsetsAPI;

public class GameService
{
    private readonly string _connectionString;
    private const int _MaxWordIndex = 7;

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
        int maxWordIndex = 2;
        foreach (var guessData in guessDataList)
        {
            int offset = (guessData.ReferenceWordIndex == _MaxWordIndex) ? (int)(guessData.GuessText.Length / 2) : gameData.Offset(guessData.ReferenceWordIndex);
            Guess guess = GetGuess(
                ++key,
                guessData.GuessText,
                gameData.ReferenceWord(guessData.ReferenceWordIndex), 
                guessData.ReferenceWordIndex, 
                offset
            );
            status.Guesses.Add(guess);

            maxWordIndex = (guess.State == GuessState.Solved) ? guess.WordIndex + 1 : guess.WordIndex;
        }

        status.State = (maxWordIndex <= _MaxWordIndex) ? GuessState.Unsolved : GuessState.Solved;
        string? refWord = null;

        if (status.State == GuessState.Unsolved)
        {
            refWord = gameData.ReferenceWord(maxWordIndex);
            int offset = (maxWordIndex == _MaxWordIndex) ? (int)(refWord.Length / 2) : gameData.Offset(maxWordIndex);
            status.NextGuess = GetGuess(++key, "", refWord, maxWordIndex, offset);
        } else {
            int[] newOrder = gameData.AnagramSortOrder();
            List<Guess> guesses = newOrder.SelectMany(i => status.Guesses.Where(g => g.WordIndex == i + 1)).ToList();
            status.Guesses = guesses;
        }
 
        // Indent should be relative to the leftmost position of the 3-letter word; see also offset
        status.Indent = Enumerable.Range(1, _MaxWordIndex)
                         .Select(x => gameData.Offset(x))
                         .Max();

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
            int played = (int)reader["played"];
            int solved = (int)reader["solved"];
            int streak = (int)reader["streakLen"];

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
        int index = 1;
        string startWord = gameData.ReferenceWord(index);
        status.Guesses.Add(GetGuess(0, startWord, startWord, index, gameData.Offset(index)));
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

    private static Guess GetGuess(int key, string guess, string referenceWord, int wordIndex, int offset)
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
            if (referenceCharCount.ContainsKey(c))
                referenceCharCount[c]++;
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

        string refWord = (guessWordIndex < _MaxWordIndex) ? gameData.ReferenceWord(guessWordIndex - 1) : gameData.ReferenceWord(_MaxWordIndex);
        var isSubset = trimGuess.GroupBy(c => c)
                                .All(g => refWord.Count(b => b == g.Key) >= g.Count());
        if (!isSubset)
        {
            errorMessage = "Not a subset";
            return false;
        }

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
    private const int _MaxWordIndex = 7;

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
        return (index == _MaxWordIndex) ? _gameDayData.Anagram : _gameDayData.SubsetWords[index - 1];
    }

    public int Offset(int index)
    {
        return (index == _MaxWordIndex) ? 0 : _gameDayData.AnagramOffsets[index - 1];
    }

    public int[] AnagramSortOrder()
    {
        var words = _gameDayData.SubsetWords;
        var offsets = _gameDayData.AnagramOffsets;
        var charToIndices = new Dictionary<char, SortedList<int, int>>();
        for (int i = 0; i < words.Length; i++) {
            string word = words[i];
            char chr = word[offsets[i] - 1];
            try {
                charToIndices[chr].Add(i, i);            
            } catch (KeyNotFoundException) {
                charToIndices[chr] = new SortedList<int, int>() { {i, i} };
            }
        }

        var anagram = _gameDayData.Anagram;
        var sortOrder = new int[anagram.Length];
        for (int i = 0; i < anagram.Length; i++)
        {
            SortedList<int, int> sortedIndices = charToIndices[anagram[i]];
            sortOrder[i] = sortedIndices.Keys[0];
            sortedIndices.RemoveAt(0);
        }
        return sortOrder;
    }

    private GameDayData GetGameDayData(DateOnly playDate)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string deltaQuery = @"
            SELECT ClueWord, Anagram, AnagramOffsets, Words
            FROM subsets
            WHERE PlayDate = @PlayDate;
        ";

        using var cmd = new NpgsqlCommand(deltaQuery, conn);
        cmd.Parameters.AddWithValue("@PlayDate", playDate);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            string clueWord = (string)reader["ClueWord"];
            string anagram = (string)reader["Anagram"];
            int[] anagramOffsets = (int[])reader["AnagramOffsets"];
            string[] words = (string[])reader["Words"];

            return new GameDayData(ClueWord: clueWord, Anagram: anagram, AnagramOffsets: anagramOffsets, SubsetWords: words);
        }

        throw new Exception("No game data today");
    }
}