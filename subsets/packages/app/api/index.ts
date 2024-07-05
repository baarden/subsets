import { Status, ClueType, Clue, Guess, GuessState, emptyGuess, Statistics } from '../types/'

const API_BASE_URL = (process.env.NODE_ENV === 'development') ? 
  'http://localhost:8080/api' : 'https://subsets.ngrok.app/api';

export const fetchStatus = async (): Promise<Status> => {
  var data: Status
  try {
    const response = await fetch(`${API_BASE_URL}/status`, {
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
        length: data.nextGuess.length,
        wordIndex: data.nextGuess.wordIndex,
        state: GuessState.Unsolved,
        offset: data.nextGuess.offset,
      }
    : emptyGuess

  return {
    clueWord: data.clueWord || '',
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
        length: guess.length,
        wordIndex: guess.wordIndex,
        state: guess.state,
        offset: guess.offset,
      })
    ),
    nextGuess: nextGuess,
    state: data.state,
    indent: data.indent,
  };
}

export const submitGuess = async (guess: string): Promise<void> => {
  const response = await fetch(`${API_BASE_URL}/guess`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ Guess: guess }),
  })

  if (response.ok) {
    return
  }
  var errorText = await response.text()
  if (response.status === 400) {
    throw new Error(errorText)
  } else {
    throw new Error(`Error: ${response.status}`)
  }
}

export const fetchStats = async () : Promise<Statistics> => {
  var data: Statistics
  try {
    const response = await fetch(`${API_BASE_URL}/stats`, {
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