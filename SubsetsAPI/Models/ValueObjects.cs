namespace SubsetsAPI.Models.ValueObjects;

public enum GuessState { Solved, Unsolved }
public enum ClueType { CorrectLetter, AllCorrect, Incorrect, Empty }

public record GameDayData(string ClueWord, string Anagram, int[] AnagramOffsets, string[] SubsetWords);
public record GuessData(string GuessText, int ReferenceWordIndex, int GuessNumber);

public class GuessPayload
{
    public required string Guess { get; set; }
}

public class Status
{
    public List<Guess> Guesses { get; set; } = new List<Guess>();
    public Guess? NextGuess { get; set; }
    public string ClueWord { get; set; } = "";
    public GuessState State { get; set; }
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
    public GuessState State { get; set; }
}

public class Clue
{
    public char Letter { get; set; }
    public ClueType Type { get; set; }
}
