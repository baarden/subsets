import { config } from '@my/ui';
import { Status, ClueType, Clue, Guess, GuessState, emptyGuess, Statistics, GameSettings } from '../types/'

export class ConflictError extends Error {
  constructor(message: string) {
      super(message);
      this.name = "ConflictError";
  }
}

const baseUrl = (config: GameSettings): string => {
  const server = (process.env.NODE_ENV === 'development') ? 
    'http://localhost:8080' : config.siteUrl;
  const url = server + config.apiPath;
  return url;
}

export const fetchStatus = async (config: GameSettings): Promise<Status> => {
  var data: Status
  try {
    const response = await fetch(`${baseUrl(config)}/status`, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
      },
    })

    if (!response.ok) {
      throw new Error(`HTTP error! Status: ${response.status}`)
    }
    data = await response.json()
  } catch (error) {
    console.error('Error fetching game status:', error)
    throw error
  }

  var nextGuess: Guess = data.nextGuess
    ? {
        key: data.nextGuess.key,
        characters: data.nextGuess.characters.map(
          (clue: any): Clue => ({
            letter: clue.letter || '',
            clueType:
              typeof clue.type === 'number' && ClueType[clue.type] !== undefined
                ? (clue.type as ClueType)
                : ClueType.Empty,
          })
        ),
        guessWord: "",
        length: data.nextGuess.length,
        wordIndex: data.nextGuess.wordIndex,
        state: GuessState.Unsolved,
        offset: data.nextGuess.offset,
        highlightLetter: ""
      }
    : emptyGuess

  return {
    clueWord: data.clueWord || '',
    characters: data.characters,
    guesses: data.guesses.map(
      (guess: any): Guess => ({
        key: guess.key,
        characters: guess.characters.map(
          (clue: any): Clue => ({
            letter: clue.letter || '',
            clueType:
              typeof clue.type === 'number' && ClueType[clue.type] !== undefined
                ? (clue.type as ClueType)
                : ClueType.Empty,
          })
        ),
        guessWord: guess.guessWord,
        length: guess.length,
        wordIndex: guess.wordIndex,
        state: guess.state,
        offset: guess.offset,
        highlightLetter: guess.highlightLetter
      })
    ),
    nextGuess: nextGuess,
    state: data.state,
    indent: data.indent,
    today: data.today
  };
}

export const submitGuess = async (guess: string, today: string, config: GameSettings): Promise<void> => {
  const response = await fetch(`${baseUrl(config)}/guess`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ Guess: guess, Date: today }),
  })

  if (response.ok) {
    return
  }
  var errorText = await response.text()
  if (response.status === 400) {
    throw new Error(errorText)
  } if (response.status === 409) {
    throw new ConflictError(errorText)
  } else {
    throw new Error(`Error: ${response.status}`)
  }
}

export const fetchStats = async (config: GameSettings) : Promise<Statistics> => {
  var data: Statistics
  try {
    const response = await fetch(`${baseUrl(config)}/stats`, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
      },
    })

    if (!response.ok) {
      throw new Error(`HTTP error! Status: ${response.status}`)
    }
    data = await response.json()
  } catch (error) {
    console.error('Error fetching statistics:', error)
    throw error
  }

  return {
    played: data.played,
    solved: data.solved,
    streak: data.streak
  }
}