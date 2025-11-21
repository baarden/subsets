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

        List<GuessData> guessDataList = FetchGuessData(userId, today, isMore);
        int maxWordIndex = 0;
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
                guessData.HighlightIndex,
                highlightLetter
            );
            guess.Hint = guessData.Hint;
            status.Guesses.Add(guess);

            maxWordIndex = (guess.State == GuessState.Solved) ? guess.WordIndex + 1 : guess.WordIndex;
        }

        status.Hints = status.Guesses.Count(g => g.Hint);

        status.State = (maxWordIndex <= config.AnagramIndex) ? GameState.Unsolved : GameState.Solved;
        string? refWord = null;

        // Collect unsolved highlight letters from AnagramSources (from maxWordIndex onwards)
        List<char> characters = new List<char>();
        string[] anagramSources = gameData.GetAnagramSources();
        for (int i = maxWordIndex; i < anagramSources.Length; i++)
        {
            string letter = anagramSources[i];
            if (!string.IsNullOrEmpty(letter) && letter.Length > 0)
            {
                characters.Add(letter[0]);
            }
        }

        // Add one deterministic extra letter based on the date (same throughout the game)
        char extraLetter = gameData.GetExtraLetter(today);
        characters.Add(extraLetter);

        // Sort the final list
        characters.Sort();
        status.Characters = characters;

        if (status.State == GameState.Unsolved)
        {
            // Special handling for AnagramIndex - create 1-character NextGuess with the extra letter
            if (maxWordIndex == config.AnagramIndex)
            {
                refWord = gameData.ReferenceWord(config.AnagramIndex);
                Guess? previousGuess = status.Guesses.LastOrDefault();

                string nextChar = " ";
                int nextHighlightIndex = 0;

                if (previousGuess?.WordIndex == config.AnagramIndex)
                {
                    nextChar = previousGuess.GuessWord[previousGuess.HighlightIndex].ToString();
                    nextHighlightIndex = previousGuess.HighlightIndex;
                }

                Guess nextGuess = GetGuess(++key, nextChar, extraLetter.ToString(), maxWordIndex, 0, nextHighlightIndex);
                nextGuess.HighlightLetter = " ";
                status.NextGuess = nextGuess;

                // Reorder guesses to spell out the previous guess (minus highlight letter)
                if (previousGuess?.WordIndex == config.AnagramIndex && !string.IsNullOrEmpty(previousGuess.GuessWord))
                {
                    int[] newOrder = gameData.GuessSortOrder(previousGuess.GuessWord, previousGuess.HighlightIndex);
                    status.Guesses = ReorderGuesses(status.Guesses, newOrder, config.AnagramIndex);
                    status.Guesses.Reverse();
                }
            }
            else
            {
                refWord = gameData.ReferenceWord(maxWordIndex);
                // Get the most recent guess and use its characters for NextGuess
                var (previousGuessWord, nextHighlightIndex) = GetNextGuessBase(status, gameData, maxWordIndex, refWord);

                // Create a guess with spaces to get Empty clue types
                string emptyGuess = new string(' ', refWord.Length);
                Guess nextGuess = GetGuess(++key, emptyGuess, refWord, maxWordIndex, 0, nextHighlightIndex);

                // Replace the space characters with previous guess characters
                for (int i = 0; i < previousGuessWord.Length && i < nextGuess.Characters.Count; i++)
                {
                    nextGuess.Characters[i].Letter = previousGuessWord[i];
                }
                nextGuess.GuessWord = previousGuessWord;

                // Determine highlight letter for NextGuess
                if (nextGuess.GuessWord.Contains(' '))
                {
                    nextGuess.HighlightLetter = " ";
                }
                else
                {
                    // Get reference word and remove its highlight letter
                    string refHighlight = gameData.HighlightLetter(maxWordIndex);
                    List<char> refChars = refWord.ToList();
                    if (!string.IsNullOrEmpty(refHighlight) && refHighlight.Length > 0)
                    {
                        refChars.Remove(refHighlight[0]);
                    }

                    // Find the extra character in nextGuess (handles duplicates correctly)
                    foreach (char c in nextGuess.GuessWord)
                    {
                        if (!refChars.Remove(c))
                        {
                            nextGuess.HighlightLetter = c.ToString();
                            break;
                        }
                    }
                }

                status.NextGuess = nextGuess;
            }
        } else {
            // Solved state: modify the anagram guess to show only the highlight character
            var anagramGuess = status.Guesses.FirstOrDefault(g =>
                g.WordIndex == config.AnagramIndex && g.State == GuessState.Solved);

            if (anagramGuess != null)
            {
                // Get the highlight character from the solved guess
                char highlightChar = anagramGuess.GuessWord[anagramGuess.HighlightIndex];

                // Modify the guess to be single-character
                anagramGuess.GuessWord = highlightChar.ToString();
                anagramGuess.Characters = new List<Clue>
                {
                    new Clue { Letter = highlightChar, Type = ClueType.AllCorrect }
                };
                anagramGuess.Length = 1;
                anagramGuess.HighlightIndex = 0;
                anagramGuess.HighlightLetter = highlightChar.ToString();
            }

            // Reorder all guesses (including the modified single-character anagram guess)
            int[] newOrder = gameData.AnagramSortOrder();
            status.Guesses = ReorderGuesses(status.Guesses, newOrder, -1);
            status.Guesses.Reverse();
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

    private static (string previousGuessWord, int highlightIndex) GetNextGuessBase(
        Status status,
        GameDayDataProvider gameData,
        int maxWordIndex,
        string refWord)
    {
        Guess? previousGuess = status.Guesses.LastOrDefault();
        string previousGuessWord = previousGuess?.GuessWord ?? "";
        int nextHighlightIndex = 0;

        // Check if this is the first guess for a new wordIndex
        bool isFirstGuessForWordIndex = previousGuess == null || previousGuess.WordIndex != maxWordIndex;

        if (isFirstGuessForWordIndex)
        {
            // First guess for this wordIndex - build sorted character set with space at end
            string currentWord = gameData.ReferenceWord(maxWordIndex);
            string highlightLetter = gameData.HighlightLetter(maxWordIndex);
            int highlightIndex = currentWord.IndexOf(highlightLetter);
            if (highlightIndex >= 0)
            {
                // Replace highlight character with space
                string wordWithSpace = currentWord.Substring(0, highlightIndex) + " " + currentWord.Substring(highlightIndex + 1);
                // Sort alphabetically with space at the end
                List<char> chars = wordWithSpace.ToList();
                List<char> nonSpaceChars = chars.Where(c => c != ' ').OrderBy(c => c).ToList();
                List<char> spaceChars = chars.Where(c => c == ' ').ToList();
                previousGuessWord = new string(nonSpaceChars.Concat(spaceChars).ToArray());
            }
            else
            {
                previousGuessWord = currentWord;
            }
            // For first guess of new wordIndex, space is at the end (based on current refWord length)
            nextHighlightIndex = refWord.Length - 1;
        }
        else
        {
            // Keep the highlight index from the most recent guess for this wordIndex
            nextHighlightIndex = previousGuess!.HighlightIndex;
        }

        return (previousGuessWord, nextHighlightIndex);
    }

    private List<GuessData> FetchGuessData(int userId, DateOnly playDate, bool isMore)
    {
        var guesses = new List<GuessData>();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string guessQuery = @"
            SELECT Guess, GuessWordIdx, GuessNumber, HighlightIdx, COALESCE(Hint, false) as Hint
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
            int highlightIndex = (int)reader["HighlightIdx"];
            bool hint = (bool)reader["Hint"];

            var data = new GuessData(
                GuessText: guessText,
                ReferenceWordIndex: referenceWordIndex,
                GuessNumber: guessNumber,
                HighlightIndex: highlightIndex,
                Hint: hint);
            guesses.Add(data);
        }

        return guesses;
    }

    private static List<Guess> ReorderGuesses(List<Guess> allGuesses, int[] sortOrder, int excludeWordIndex)
    {
        List<Guess> reordered = sortOrder
            .SelectMany(i => allGuesses.Where(g => g.WordIndex == i && g.WordIndex != excludeWordIndex))
            .ToList();

        var excluded = allGuesses.Where(g => g.WordIndex == excludeWordIndex).ToList();

        return reordered.Concat(excluded).ToList();
    }

    private static Guess GetGuess(
        int key,
        string guess,
        string referenceWord,
        int wordIndex,
        int offset,
        int highlightIndex = 0,
        string highlightLetter="")
    {
        var newGuess = new Guess
        {
            Key = key,
            GuessWord = guess,
            WordIndex = wordIndex,
            Length = referenceWord.Length,
            Offset = offset,
            HighlightIndex = highlightIndex,
            Characters = GetClues(guess, referenceWord, highlightIndex, highlightLetter),
            State = GuessState.Unsolved
        };

        if (newGuess.Characters.All(c => c.Type == ClueType.AllCorrect))
        {
            newGuess.State = GuessState.Solved;
            newGuess.HighlightLetter = highlightLetter;
        }
        return newGuess;
    }

    private static List<Clue> GetClues(string guess, string referenceWord, int highlightIndex = -1, string highlightLetter = "")
    {
        var clues = new List<Clue>();

        for (int i = 0; i < referenceWord.Length; i++)
        {
            char guessChar = i < guess.Length ? guess[i] : ' ';
            ClueType type;

            // Direct check for highlight position: if this is the highlight character and it doesn't match, it's incorrect
            if (i == highlightIndex && !string.IsNullOrEmpty(highlightLetter) && guessChar != highlightLetter[0])
            {
                type = ClueType.Incorrect;
            }
            else if (guessChar == referenceWord[i])
            {
                // Character is in the correct position
                type = ClueType.AllCorrect;
            }
            else if ((i > 0 && guessChar == referenceWord[i - 1]) ||
                     (i < referenceWord.Length - 1 && guessChar == referenceWord[i + 1]))
            {
                // Character appears in an adjacent position (left or right)
                type = ClueType.Adjacent;
            }
            else if (referenceWord.Contains(guessChar))
            {
                // Character exists in the word but is not correct or adjacent
                type = ClueType.Empty;
            }
            else
            {
                // Character doesn't exist in the reference word
                type = ClueType.Incorrect;
            }

            clues.Add(new Clue { Letter = guessChar, Type = type });
        }

        return clues;
    }

    public (char, int, string?) GenerateHint(int userId, DateOnly guessDate, bool isMore)
    {
        try
        {
            // Get current game status
            var (status, refWord) = GetStatus(userId, guessDate, isMore);

            if (status.NextGuess == null || refWord == null)
            {
                return ('\0', 0, "No active word to hint for");
            }

            int wordIndex = status.NextGuess.WordIndex;
            var gameData = new GameDayDataProvider(guessDate, isMore, _connectionString);
            string referenceWord = gameData.ReferenceWord(wordIndex);
            string highlightLetter = gameData.HighlightLetter(wordIndex);

            // Get the base guess (same logic as NextGuess)
            var (previousGuessWord, currentHighlightIndex) = GetNextGuessBase(status, gameData, wordIndex, referenceWord);

            // Generate clues for the current guess (needed for finding target characters)
            List<Clue> currentClues = GetClues(previousGuessWord, referenceWord, currentHighlightIndex, highlightLetter);

            // Check all previous guesses to find positions that have EVER been AllCorrect
            HashSet<int> positionsWithAllCorrect = new HashSet<int>();
            foreach (var guess in status.Guesses.Where(g => g.WordIndex == wordIndex))
            {
                for (int i = 0; i < guess.Characters.Count; i++)
                {
                    if (guess.Characters[i].Type == ClueType.AllCorrect)
                    {
                        positionsWithAllCorrect.Add(i);
                    }
                }
            }

            // Find eligible clue positions: never received AllCorrect
            List<int> eligiblePositions = new List<int>();
            for (int i = 0; i < referenceWord.Length; i++)
            {
                if (!positionsWithAllCorrect.Contains(i))
                {
                    eligiblePositions.Add(i);
                }
            }

        if (eligiblePositions.Count == 0)
        {
            return ('\0', 0, "No more hints are available");
        }

        // Shuffle eligible positions and try each until we find one with a valid target character
        Random random = new Random();
        var shuffledPositions = eligiblePositions.OrderBy(x => random.Next()).ToList();

        int cluePosition = -1;
        int targetIndex = -1;
        char correctChar = '\0';

        foreach (int pos in shuffledPositions)
        {
            correctChar = referenceWord[pos];

            // Find target character: matching correctChar, not at AllCorrect position, not at highlightIndex
            for (int i = 0; i < previousGuessWord.Length; i++)
            {
                if (i != currentHighlightIndex &&
                    previousGuessWord[i] == correctChar &&
                    currentClues[i].Type != ClueType.AllCorrect)
                {
                    targetIndex = i;
                    cluePosition = pos;
                    break;
                }
            }

            if (targetIndex != -1)
            {
                break;
            }
        }

        if (targetIndex == -1)
        {
            return ('\0', 0, "No more hints are available.");
        }

        // Shift the character from targetIndex to cluePosition
        List<char> guessChars = previousGuessWord.ToList();
        char targetChar = guessChars[targetIndex];

        // Remove from source position
        guessChars.RemoveAt(targetIndex);

        // Insert at destination position
        // When moving right (targetIndex < cluePosition), after removal, the original cluePosition
        // still points to where we want to insert (after what was there)
        guessChars.Insert(cluePosition, targetChar);

        string hintGuess = new string(guessChars.ToArray());

        // Update highlightIndex if it was shifted by the move
        int newHighlightIndex = currentHighlightIndex;
        if (targetIndex < cluePosition)
        {
            // Character moved right, positions in between shift left
            if (currentHighlightIndex > targetIndex && currentHighlightIndex <= cluePosition)
            {
                newHighlightIndex = currentHighlightIndex - 1;
            }
        }
        else
        {
            // Character moved left, positions in between shift right
            if (currentHighlightIndex >= cluePosition && currentHighlightIndex < targetIndex)
            {
                newHighlightIndex = currentHighlightIndex + 1;
            }
        }

        // Save the hint guess
        int guessNumber = status.Guesses.Count + 1;
        var (inserted, errorMessage) = AddHintGuess(
            userId,
            guessDate,
            guessNumber,
            wordIndex,
            hintGuess,
            newHighlightIndex,
            isMore);

            if (!inserted)
            {
                return ('\0', 0, errorMessage ?? "Failed to save hint");
            }

            return (targetChar, cluePosition, null);
        }
        catch (Exception e)
        {
            return ('\0', 0, e.Message);
        }
    }

    public (bool, string?) AddGuess(
        int userId,
        DateOnly guessDate,
        int guessNumber,
        int guessWordIndex,
        string guess,
        int highlightIndex,
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
        if (count == 0) { return (false, $"'{trimGuess}' is not in the word list"); }

        Guess guessData = GetGuess(0, guess, refWord, guessWordIndex, 0, highlightIndex);
        bool solved = guessData.Characters.All(c => c.Type == ClueType.AllCorrect);

        string insertQuery = @"
            INSERT INTO Guess (UserId, IsMore, GuessDate, GuessNumber, GuessWordIdx, Guess, HighlightIdx, Solved, Hint)
            VALUES (@UserId, @IsMore, @GuessDate, @GuessNumber, @GuessWordIndex, @Guess, @HighlightIdx, @Solved, @Hint);
        ";

        using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@IsMore", isMore);
        cmd.Parameters.AddWithValue("@GuessDate", guessDate);
        cmd.Parameters.AddWithValue("@GuessNumber", guessNumber);
        cmd.Parameters.AddWithValue("@GuessWordIndex", guessWordIndex);
        cmd.Parameters.AddWithValue("@Guess", guess);
        cmd.Parameters.AddWithValue("@HighlightIdx", highlightIndex);
        cmd.Parameters.AddWithValue("@Solved", solved);
        cmd.Parameters.AddWithValue("@Hint", false);

        int result = cmd.ExecuteNonQuery();
        return (result == 1, null);
    }

    private (bool, string?) AddHintGuess(
        int userId,
        DateOnly guessDate,
        int guessNumber,
        int guessWordIndex,
        string guess,
        int highlightIndex,
        bool isMore)
    {
        var gameData = new GameDayDataProvider(guessDate, isMore, _connectionString);
        string refWord = gameData.ReferenceWord(guessWordIndex);

        Guess guessData = GetGuess(0, guess, refWord, guessWordIndex, 0, highlightIndex);
        bool solved = guessData.Characters.All(c => c.Type == ClueType.AllCorrect);

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string insertQuery = @"
            INSERT INTO Guess (UserId, IsMore, GuessDate, GuessNumber, GuessWordIdx, Guess, HighlightIdx, Solved, Hint)
            VALUES (@UserId, @IsMore, @GuessDate, @GuessNumber, @GuessWordIndex, @Guess, @HighlightIdx, @Solved, @Hint);
        ";

        using var cmd = new NpgsqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@IsMore", isMore);
        cmd.Parameters.AddWithValue("@GuessDate", guessDate);
        cmd.Parameters.AddWithValue("@GuessNumber", guessNumber);
        cmd.Parameters.AddWithValue("@GuessWordIndex", guessWordIndex);
        cmd.Parameters.AddWithValue("@Guess", guess);
        cmd.Parameters.AddWithValue("@HighlightIdx", highlightIndex);
        cmd.Parameters.AddWithValue("@Solved", solved);
        cmd.Parameters.AddWithValue("@Hint", true);

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

    public char GetExtraLetter(DateOnly date)
    {
        // Collect all characters from reference words (indices 0 through LastPlusOneIndex)
        List<char> allChars = new List<char>();
        for (int i = 0; i <= _config.LastPlusOneIndex; i++)
        {
            allChars.AddRange(_gameDayData.SubsetWords[i]);
        }

        // Use date hash to pick a deterministic index
        int dateHash = date.GetHashCode();
        int index = Math.Abs(dateHash) % allChars.Count;

        return allChars[index];
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

    public int[] GuessSortOrder(string targetGuess, int highlightIndex)
    {
        List<string> charSources = _gameDayData.AnagramSources.ToList();

        var sortOrder = new List<int>();
        for (int i = 0; i < targetGuess.Length; i++)
        {
            if (i == highlightIndex) continue; // Skip highlight position

            char c = targetGuess[i];
            int idx = charSources.IndexOf(c.ToString());
            if (idx >= 0)
            {
                sortOrder.Add(idx);
                charSources[idx] = ""; // Mark as used for duplicates
            }
        }
        return sortOrder.ToArray();
    }

    public string[] GetAnagramSources()
    {
        return _gameDayData.AnagramSources;
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