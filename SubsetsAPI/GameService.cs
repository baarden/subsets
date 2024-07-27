using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    public (Status, string?) GetStatus(int userId, DateOnly today, bool isMore)
    {
        AppSettings config = isMore ? PlusOneMoreConfig : PlusOneConfig;
        var status = new Status { Today = today };

        var gameData = new GameDayDataProvider(today, isMore, _connectionString);
        status.ClueWord = gameData.ClueWord;

        int key = 0;
        AddStartingGuess(status, gameData);

        List<GuessData> guessDataList = FetchGuessData(userId, today, isMore);
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

            if (guess.State == GuessState.Solved && maxWordIndex == config.ExtraLetterIndex) {
                string finalLetter = gameData.HighlightLetter(config.ExtraLetterIndex);
                Guess finalGuess = GetGuess(++key, finalLetter, finalLetter, config.ExtraLetterIndex, 1, finalLetter);
                status.Guesses.Add(finalGuess);
                maxWordIndex++;
            }
        }

        status.State = (maxWordIndex <= config.AnagramIndex) ? GameState.Unsolved : GameState.Solved;
        string? refWord = null;

        int refCharIdx = (maxWordIndex == config.AnagramIndex) ? config.AnagramIndex : config.LastPlusOneIndex;
        List<char> gameChars = gameData.ReferenceWord(refCharIdx).ToList(); 
        string prevWord = (maxWordIndex == config.AnagramIndex) ? "" : gameData.ReferenceWord(maxWordIndex - 1);
        List<char> group1 = prevWord.ToList();
        Dictionary<char, int> prevCharCounts = prevWord.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        List<char> group2 = gameChars.Where(c => !prevCharCounts.ContainsKey(c) || prevCharCounts[c]-- <= 0).ToList();
        group1.Sort();
        group2.Sort();
        status.Characters = group1.Concat(group2).ToList();

        if (status.State == GameState.Unsolved)
        {
            refWord = gameData.ReferenceWord(maxWordIndex);
            status.NextGuess = GetGuess(++key, "", refWord, maxWordIndex, 0);
        } else {
            int[] newOrder = gameData.AnagramSortOrder();
            List<Guess> guesses = newOrder.SelectMany(i => status.Guesses.Where(g => g.WordIndex == i)).ToList();
            var anagramGuesses = status.Guesses.Where(g => g.WordIndex == config.AnagramIndex).ToList();
            status.Guesses = guesses.Concat(anagramGuesses).ToList();
        }
 
        status.Indent = 0;

        return (status, refWord);
    }

    public Statistics GetStatistics(int userId, DateOnly today, bool isMore)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string guessQuery = @"
            with guessdata as (
                select guessdate, cast(solved as int) solved, 1 played
                    from guess
                    where userid = @userId
                        and isMore = @isMore
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
        cmd.Parameters.AddWithValue("@isMore", isMore);
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

    private static void AddStartingGuess(Status status, GameDayDataProvider gameData)
    {
        int index = 0;
        string startWord = gameData.ReferenceWord(index);
        string highlightLetter = gameData.HighlightLetter(index);
        status.Guesses.Add(GetGuess(0, startWord, startWord, index, 0, highlightLetter));
    }

    private List<GuessData> FetchGuessData(int userId, DateOnly playDate, bool isMore)
    {
        var guesses = new List<GuessData>();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string guessQuery = @"
            SELECT Guess, GuessWordIdx, GuessNumber
            FROM Guess
            WHERE UserId = @UserId
                AND IsMore = @IsMore
                AND GuessDate = @PlayDate
            ORDER BY GuessNumber;
        ";

        using var cmd = new NpgsqlCommand(guessQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@IsMore", isMore);
        cmd.Parameters.AddWithValue("@PlayDate", playDate);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string guessText = (string)reader["Guess"];
            int referenceWordIndex = (int)reader["GuessWordIdx"];
            int guessNumber = (int)reader["GuessNumber"];

            var data = new GuessData(
                GuessText: guessText,
                ReferenceWordIndex: referenceWordIndex,
                GuessNumber: guessNumber);
            guesses.Add(data);
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

    public (bool, string?) AddGuess(
        int userId,
        DateOnly guessDate,
        int guessNumber,
        int guessWordIndex,
        string guess,
        bool isMore)
    {
        string trimGuess = guess.Trim();

        var gameData = new GameDayDataProvider(guessDate, isMore, _connectionString);
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
        if (count == 0) { return (false, "Not in the word list"); }

        Guess guessData = GetGuess(0, guess, refWord, guessWordIndex, 0);
        bool solved = guessData.Characters.All(c => c.Type == ClueType.AllCorrect);

        string insertQuery = @"
            INSERT INTO Guess (UserId, IsMore, GuessDate, GuessNumber, GuessWordIdx, Guess, Solved)
            VALUES (@UserId, @IsMore, @GuessDate, @GuessNumber, @GuessWordIndex, @Guess, @Solved);
        ";

        using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@IsMore", isMore);
        cmd.Parameters.AddWithValue("@GuessDate", guessDate);
        cmd.Parameters.AddWithValue("@GuessNumber", guessNumber);
        cmd.Parameters.AddWithValue("@GuessWordIndex", guessWordIndex);
        cmd.Parameters.AddWithValue("@Guess", guess);
        cmd.Parameters.AddWithValue("@Solved", solved);

        int result = cmd.ExecuteNonQuery();
        return (result == 1, null);
    }

}

public class GameDayDataProvider
{
    private readonly string PlusOneSetTable = "plusone";
    private readonly string PlusOneMoreSetTable = "plusonemore";
    public readonly string ClueWord;
    private readonly string _connectionString;
    private readonly GameDayData _gameDayData;
    private readonly AppSettings _config;

    public GameDayDataProvider(DateOnly playDate, bool isMore, string connectionString)
    {
        _connectionString = connectionString;
        _config = isMore ? PlusOneMoreConfig : PlusOneConfig;
        string setTable = isMore ? PlusOneMoreSetTable : PlusOneSetTable;
        _gameDayData = GetGameDayData(playDate, setTable);
        ClueWord = _gameDayData.ClueWord;
    }

    public string ReferenceWord(int index)
    {
        return (index == _config.AnagramIndex) ? _gameDayData.Anagram : _gameDayData.SubsetWords[index];
    }

    public int Offset(int index)
    {
        return 1;
    }

    public string HighlightLetter(int wordIndex)
    {
        if (wordIndex == _config.AnagramIndex) { return ""; }
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

    private GameDayData GetGameDayData(DateOnly playDate, string setTable)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string deltaQuery = @$"
            SELECT ClueWord, Anagram, AnagramSources, Words
            FROM {setTable}
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

            return new GameDayData(
                ClueWord: clueWord,
                Anagram: anagram,
                AnagramSources: anagramSources,
                SubsetWords: words);
        }

        throw new Exception("No game data today");
    }
}