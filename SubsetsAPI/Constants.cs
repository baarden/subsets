
namespace SubsetsAPI;

public static class Constants
{
    public enum GuessState { Solved, Unsolved }
    public enum GameState { Solved, Unsolved }
    public enum ClueType { CorrectLetter, AllCorrect, Incorrect, Empty }

    public const int StartWordIndex = 0;
    public const int LastPlusOneIndex = 5;
    public const int ExtraLetterIndex = 6;
    public const int AnagramIndex = 7;
}