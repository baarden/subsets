using static SubsetsAPI.Constants;

namespace SubsetsAPI.Models;

public record GameDayData(string ClueWord, string Anagram, string[] AnagramSources, string[] SubsetWords);
public record GuessData(string GuessText, int ReferenceWordIndex, int GuessNumber, int HighlightIndex);
public record AppSettings (
    int LastPlusOneIndex,
    int AnagramIndex
);
public class GuessRequest
{
    public required string Guess { get; set; }
    public required DateOnly Date { get; set; }
    public required int HighlightIndex { get; set; }
}

public class Status
{
    public List<Guess> Guesses { get; set; } = new List<Guess>();
    public Guess? NextGuess { get; set; }
    public string ClueWord { get; set; } = "";
    public List<char> Characters { get; set; } = new List<char>();
    public GameState State { get; set; }
    public DateOnly Today { get; set; }
    public int Indent { get; set; }
}

public class Guess
{
    public int Key { get; set; }
    public string GuessWord { get; set; } = string.Empty;
    public List<Clue> Characters { get; set; } = new List<Clue>();
    public int WordIndex { get; set; }
    public int Length { get; set; }
    public int Offset { get; set; }
    public string HighlightLetter { get; set; } = "";
    public int HighlightIndex { get; set; }
    public GuessState State { get; set; }
}

public class Clue
{
    public char Letter { get; set; }
    public ClueType Type { get; set; }
}

public class Statistics 
{
    public int Played { get; set; }
    public int Solved { get; set; }
    public int Streak { get; set; }
}

public class GuessResponse
{
    public DateOnly Date { get; set; }
}
