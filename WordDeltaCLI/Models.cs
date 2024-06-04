using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordDeltaCLI;

internal class Models
{
    public enum GuessState { Solved, Unsolved }
    public enum ClueType { CorrectLetter, AllCorrect, Incorrect, Empty }

    public class Status
    {
        public List<Guess> Guesses { get; set; } = new List<Guess>();
        public Guess? NextGuess { get; set; }
        public string ClueWord { get; set; } = "";
        public GuessState State { get; set; } = GuessState.Unsolved;
        public DateOnly Today { get; set; }
        public int Indent { get; set; }
    }

    public class Guess
    {
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

}
