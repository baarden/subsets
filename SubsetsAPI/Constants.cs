
using SubsetsAPI.Models;

namespace SubsetsAPI;

public class Constants
{
    public enum GuessState { Solved, Unsolved }
    public enum GameState { Solved, Unsolved }
    public enum ClueType { CorrectLetter, AllCorrect, Incorrect, Empty }
    public const int StartWordIndex = 0;

    public static readonly AppSettings PlusOneConfig = new(
        LastPlusOneIndex: 4,
        ExtraLetterIndex: 5,
        AnagramIndex: 6
    );
    public static readonly AppSettings PlusOneMoreConfig = new(
        LastPlusOneIndex: 5,
        ExtraLetterIndex: 6,
        AnagramIndex: 7
    );
}