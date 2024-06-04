using System;
using System.Collections.Generic;
using Npgsql;
using WordDeltaAPI.Models.ValueObjects;

namespace WordDeltaAPI;

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
        var status = new Status();
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        DateOnly today = DateOnly.FromDateTime(now);
        status.Today = today;

        var gameData = new GameDayDataProvider(today, _connectionString);
        status.ClueWord = gameData.ClueWord;
        status.Guesses.Add(GetGuess(gameData.ReferenceWord(1), gameData.ReferenceWord(1), 1, gameData.Offset(1)));

        var guessDataList = FetchGuessData(userId, today);
        int maxWordIndex = 2;
        foreach (var guessData in guessDataList)
        {
            int offset = (guessData.ReferenceWordIndex == 7) ? (int)(guessData.GuessText.Length / 2) : gameData.Offset(guessData.ReferenceWordIndex);
            Guess guess = GetGuess(
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
            int offset = (maxWordIndex == 7) ? (int)(refWord.Length / 2) : gameData.Offset(maxWordIndex);
            status.NextGuess = GetGuess("", refWord, maxWordIndex, offset);
        }

        status.Indent = Enumerable.Range(1, 6)
                         .Select(x => gameData.Offset(x))
                         .Max();

        return (status, refWord);
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

    private static Guess GetGuess(string guess, string referenceWord, int wordIndex, int offset)
    {
        var newGuess = new Guess
        {
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

            correctLetterCount[c] = 0; // Initialize correct letter counts
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
                // Check if the correct letter count has not exceeded its allowed maximum
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

        var gameData = new GameDayDataProvider(guessDate, _connectionString);
        string prevWord = gameData.ReferenceWord(guessWordIndex - 1);

        var isSubset = prevWord.GroupBy(c => c)
                                .All(g => guess.Count(b => b == g.Key) >= g.Count());
        if (!isSubset && guessWordIndex < 7)
        {
            errorMessage = "Missing letters from previous word";
            return false;
        }

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string checkGuessQuery = @"
            SELECT COUNT(*)
            FROM Words w
                LEFT JOIN NonWords nw on nw.NonWord = w.Word
            WHERE w.Word = @Guess
                AND nw.Id IS NULL;
        ";

        using var checkCmd = new NpgsqlCommand(checkGuessQuery, conn);
        checkCmd.Parameters.AddWithValue("@Guess", guess);

        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (count == 0)
        {
            errorMessage = "Not in the word list";
            return false;
        }

        string duplicateQuery = @"
            select 1
            from guess
            where userid = @userId
                and guessdate = @playDate
                and guess = @guess;
        ";

        using var duplicateCmd = new NpgsqlCommand(duplicateQuery, conn);
        duplicateCmd.Parameters.AddWithValue("@userId", userId);
        duplicateCmd.Parameters.AddWithValue("@playDate", guessDate);
        duplicateCmd.Parameters.AddWithValue("@guess", guess);

        var duplicateCount = Convert.ToInt32(duplicateCmd.ExecuteScalar());
        if (duplicateCount > 0)
        {
            errorMessage = "Already guessed";
            return false;
        }

        string refWord = gameData.ReferenceWord(guessWordIndex);
        Guess guessData = GetGuess(guess, refWord, guessWordIndex, 0);
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
        return (index == 7) ? _gameDayData.Anagram : _gameDayData.DeltaWords[index - 1];
    }

    public int Offset(int index)
    {
        return (index == 7) ? 0 : _gameDayData.AnagramOffsets[index - 1];
    }

    private GameDayData GetGameDayData(DateOnly playDate)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string deltaQuery = @"
            SELECT ClueWord, Anagram, AnagramOffsets, DeltaWords
            FROM Deltas
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
            string[] deltaWords = (string[])reader["DeltaWords"];

            return new GameDayData(ClueWord: clueWord, Anagram: anagram, AnagramOffsets: anagramOffsets, DeltaWords: deltaWords);
        }

        throw new Exception("Unable to get data for requested date");
    }
}