
export enum ClueType {
  CorrectLetter = 0,
  AllCorrect = 1,
  Incorrect = 2,
  Empty = 3,
  Anagram = 4
}

export enum GuessState {
  Solved = 0,
  Unsolved = 1
}

export enum GameState {
  Solved = 0,
  Unsolved = 1
}

// Represents the evaluation of a particular letter in a guess
export interface Clue {
  letter: string;
  clueType: ClueType;
}

// Represents a single guess, which is a sequence of clues
export interface Guess {
  key: number;
  characters: Clue[];
  length: number;
  wordIndex: number;
  state: GuessState;
  offset: number;
}

// The main game status returned from the API
export interface Status {
  clueWord: string;   // The word used as a clue for the current game
  guesses: Guess[];   // Array of previous guesses with clues
  nextGuess: Guess;   // Structure for the next guess (mainly used for its length)
  state: GameState;   // Current state of the game (solved/unsolved)
  indent: number;
}
