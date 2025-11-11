
using SubsetsAPI.Models;

namespace SubsetsAPI;

public class Constants
{
    public enum GuessState { Solved, Unsolved }
    public enum GameState { Solved, Unsolved }
    public enum ClueType { Adjacent, AllCorrect, Incorrect, Empty }
    public const int StartWordIndex = 0;

    public static readonly AppSettings PlusOneConfig = new(
        LastPlusOneIndex: 4,
        AnagramIndex: 5
    );
    public static readonly AppSettings PlusOneMoreConfig = new(
        LastPlusOneIndex: 5,
        AnagramIndex: 6
    );
}