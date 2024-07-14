
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

export interface Clue {
  letter: string;
  clueType: ClueType;
}

export interface Guess {
  key: number;
  characters: Clue[];
  guessWord: string;
  length: number;
  wordIndex: number;
  state: GuessState;
  offset: number;
  highlightLetter: string;
}

export interface Status {
  clueWord: string;   // The word used as a clue for the current game
  guesses: Guess[];   // Array of previous guesses with clues
  nextGuess: Guess;   // Structure for the next guess (mainly used for its length)
  characters: string[];
  state: GameState;   // Current state of the game (solved/unsolved)
  indent: number;
}

export interface Statistics {
  played: number,
  solved: number,
  streak: number
}

export const emptyGuess: Guess = {
  key: 0,
  characters: [],
  guessWord: "",
  offset: 0,
  highlightLetter: "",
  length: 0,
  wordIndex: 0,
  state: GuessState.Unsolved,
}
