import type { NextApiRequest, NextApiResponse } from 'next'

import { Status, ClueType, Clue, Guess, GuessState } from '../../types'

//const API_BASE_URL = 'https://moosibou.ngrok.app/api';
const API_BASE_URL = 'http://localhost:5102/api'

export const fetchStatus = async (req: NextApiRequest, res: NextApiResponse): Promise<Status> => {
  var data: Status
  try {
    const response = await fetch(`${API_BASE_URL}/status`, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
        Cookie: req.headers.cookie || '',
      },
      credentials: 'include',
    })

    if (!response.ok) {
      throw new Error(`HTTP error! Status: ${response.status}`)
    }
    data = await response.json()
    const cookies = response.headers.get('set-cookie')
    if (cookies) {
      res.setHeader('Set-Cookie', cookies.split(','))
    }
  } catch (error) {
    console.error('Error fetching game status:', error)
    throw error
  }

  var nextGuess = data.nextGuess
    ? {
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
    : {
        characters: [],
        length: 0,
        wordIndex: 0,
        state: GuessState.Solved,
        offset: 0,
      }

  var output = {
    clueWord: data.clueWord || '',
    guesses: data.guesses.map(
      (guess: any): Guess => ({
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
  }
  res.status(200).json(output)
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
