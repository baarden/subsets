
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

export enum KeyboardAction {
  Keep = 1,
  Unset = 0,
  Delete = -1
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
  clueWord: string; 
  guesses: Guess[]; 
  nextGuess: Guess; 
  characters: string[];
  state: GameState;
  indent: number;
  today: string;
}

export interface Statistics {
  played: number,
  solved: number,
  streak: number
}

export interface ExampleText {
  startWord: string,
  exampleWord: string,
  correctLetters: string,
  wrongLetters: string,
  nonLetters: string,
  anagram: string
}

export interface ScoringRange {
  min: number;
  max: number;
  message: string;
}

export interface GameSettings {
  siteUrl: string,
  sitePath: string,
  apiPath: string,
  gameName: string,
  anagramIndex: number,
  scoreRanges: ScoringRange[],
  exampleText: ExampleText,
  logoImagePath: string,
  fullExampleImagePath: string,
  exampleImagePath: string,
  anagramImagePath: string,
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
