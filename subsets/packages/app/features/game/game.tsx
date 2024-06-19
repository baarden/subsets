import React, { useState, useEffect, useRef } from 'react'
import { YStack, Text, ScrollView, Stack, Theme } from 'tamagui'
import {} from '@my/ui/src/'
import TitleBar from './titlebar'
import GuessRow from './guessrow'
import Keyboard, { KeyboardHandles } from './keyboard'
import Drawer from './drawer'
import { fetchStatus, submitGuess } from '../api'
import { GuessState, Status, Guess, GameState } from '../../types'

const emptyGuess: Guess = {
  key: 0,
  characters: [],
  offset: 0,
  length: 0,
  wordIndex: 0,
  state: GuessState.Unsolved,
}
const squareWidth: number = 22
const anagramGuess: number = 7
const backspace = '\u232B'
const shuffle = '\uD83D\uDD00'

export function GameComponent() {
  const [currentGuess, setCurrentGuess] = useState<Guess>(emptyGuess)
  const [status, setStatus] = useState<Status | null>(null)
  const [error, setError] = useState<string>('')
  const [visibleWordIndices, setVisibleWordIndices] = useState<Set<number>>(new Set<number>())
  const [editableIndex, setEditableIndex] = useState(0)
  const [keyboardLayout, setKeyboardLayout] = useState<string[][]>([[]])
  const [drawerVisible, setDrawerVisible] = useState(false);

  const keyboardRef = useRef<KeyboardHandles>(null);  

  const handleSquareSelected = (index) => {
    setEditableIndex(index)
  }

  useEffect(() => {
    setDrawerVisible(true)
    fetchStatus()
      .then((statusData) => {
        updateStatus(statusData)
      })
      .catch((err) => setError('Failed to fetch game status'))
  }, [])

  const updateStatus = (statusData: Status) => {
    if (!statusData) {
      setError('')
      return
    }
    setStatus(statusData)
    setEditableIndex(0)
    let keys: string[][] = getKeys(statusData.guesses, statusData.nextGuess.wordIndex, statusData.state)
    setKeyboardLayout(keys)
    var nextGuess = statusData.nextGuess
    setCurrentGuess(nextGuess)
    var prevGuess = statusData.guesses.slice(-1)[0]
    if (nextGuess.wordIndex == 7 && prevGuess.length < 7) {
      setError('Find a channel anagram matching the clue!')
      setTimeout(() => setError(''), 3000)
    } else if (statusData.state == GameState.Solved) {
      setError('Solved!')
    } else {
      setError('')
    }
  }

  const getKeys = (guesses: Guess[], wordIndex: number, gameState: GameState): string[][] => {
    const secondRow = ['ENTER', shuffle, backspace];
    if (gameState == GameState.Solved) { return [[]] }
    let firstRow: string[] = []
    for (let i = guesses.length - 1; i >= 0; i--) {
      let guess : Guess = guesses[i];
      if (wordIndex == anagramGuess) {
        if (guess.state == GuessState.Solved) {
          firstRow.push(guess.characters[guess.offset - 1].letter.toUpperCase());
        }
      } else if (guess.state == GuessState.Solved) {
        let letters: string[] = guess.characters.map(c => c.letter.toUpperCase());
        return [letters.sort(), secondRow];
      }
    }
    return [firstRow.reverse(), secondRow];
  }  

  const handleKeyPress = async (key: string) => {
    if (status === null) return
    if (key === 'ENTER') {
      if (currentGuessLength() >= 3) {
        try {
          await submitGuess(currentGuess.characters.map((clue) => clue.letter).join(''))
          const statusData = await fetchStatus()
          updateStatus(statusData)
        } catch (err) {
          setError(err.message)
          setTimeout(() => setError(''), 3000)
        }
      } else {
        setError('Incomplete guess')
        setTimeout(() => setError(''), 2000)
      }
    } else if (key == shuffle) {
      let keys = [shuffleArray(keyboardLayout[0]), keyboardLayout[1]]
      setKeyboardLayout(keys)
      currentGuess.characters.forEach(clue => {
        clue.letter = ' '
      })
      setEditableIndex(0)
    } else if (key === backspace) {
      let deletedChar = currentGuess.characters[editableIndex].letter
      updateGuessCharacter(editableIndex, ' ')
      if (editableIndex > 0) {
        setEditableIndex(editableIndex - 1)
      }
      keyboardRef.current?.enableKey(deletedChar.toUpperCase())
    } else if (editableIndex >= 0) {
      let deletedChar = currentGuess.characters[editableIndex].letter
      if (deletedChar !== ' ') {
        keyboardRef.current?.enableKey(deletedChar.toUpperCase())
      }
      updateGuessCharacter(editableIndex, key)
      setEditableIndex(nextEditableIndex())
    }
  }

  function shuffleArray(array) {
    let shuffledArray = array.slice();
    for (let i = shuffledArray.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [shuffledArray[i], shuffledArray[j]] = [shuffledArray[j], shuffledArray[i]];
    }
    return shuffledArray;
  }

  const currentGuessLength = (): number => {
    var chars = currentGuess.characters
    var guessStart = -1
    var guessEnd = -1
    for (let i = 0; i < chars.length; i++) {
      if (chars[i].letter == ' ') {
        if (guessStart >= 0 && guessEnd < 0) {
          guessEnd = i
        }
      } else if (guessEnd >= 0) {
        return 0
      } else if (guessStart < 0) {
        guessStart = i
      }
    }
    if (guessStart < 0) {
      return 0
    }
    if (guessEnd < 0) {
      guessEnd = chars.length;
    }
    return guessEnd - guessStart
  }

  const nextEditableIndex = (): number => {
    var chars = currentGuess.characters
    for (let i = editableIndex + 1; i < chars.length; i++) {
      if (chars[i].letter == ' ') {
        return i
      }
    }
    for (let i = 0; i < editableIndex; i++) {
      if (chars[i].letter == ' ') {
        return i
      }
    }
    return (editableIndex < chars.length - 1) ? editableIndex + 1 : editableIndex;
  }

  const updateGuessCharacter = (index: number, newLetter: string) => {
    setCurrentGuess((prev) => {
      const newCharacters = [...prev.characters]
      newCharacters[index] = { ...newCharacters[index], letter: newLetter }
      return { ...prev, characters: newCharacters }
    })
  }

  const handleDrawerClose = () => {
    setDrawerVisible(false);
  };

  const handleInfoPress = () => {
    setDrawerVisible(true);
  };

  const renderError = () => {
    if (!error) {
      return null
    }
    return (
      <Stack
        alignSelf="center"
        position="absolute"
        top={ status?.state == GameState.Solved ? 0 : -1 * squareWidth - 5}
        bg="black"
        p="$2"
        zIndex={2}
        alignItems="center" // Center children horizontally
      >
        <Text color="white" fontSize="$1">
          {error}
        </Text>
      </Stack>
    )
  }

  const renderSpacer = () => (
    <Stack
      width={200}
      height={10} 
      backgroundColor="darkgrey"
      margin={4}
      borderRadius={5}
      alignSelf='center'
      />
  );
  
  const renderGuessRow = (guess, shouldInsertSpacer) => (
    <>
      <GuessRow
        key={'guessrow_' + guess.key}
        style={{ display: 'flex' }}
        guess={guess}
        onRowPress={() => toggleVisibility(guess.wordIndex)}
        isSolved={ guess.state === GuessState.Solved }
        isEditable={false}
        squareDim={squareWidth}
      />
      {shouldInsertSpacer && renderSpacer()}
      </>
  );
  
  const renderGuessRows = () => {
    if (!status) {
      return null;
    }
    
    return (
      <YStack>
        {status.guesses.map((guess, index) => {
          const isVisible =
            visibleWordIndices.has(guess.wordIndex) ||
            guess.wordIndex === status.nextGuess.wordIndex ||
            guess.state === GuessState.Solved;
          if (!isVisible) {
            return null;
          }
  
          const shouldInsertSpacer =
            status.state != GameState.Solved
            && guess.state == GuessState.Solved
            && guess.wordIndex === anagramGuess - 1
  
          return renderGuessRow(guess, shouldInsertSpacer);
        })}
      </YStack>
    );
  }

  const toggleVisibility = (wordIndex: number) => {
    setVisibleWordIndices((prevIndices) => {
      const newIndices = new Set(prevIndices)
      if (newIndices.has(wordIndex)) {
        newIndices.delete(wordIndex)
      } else {
        newIndices.add(wordIndex)
      }
      return newIndices
    })
  }

  const ScoringPanel = () => (
    <Stack
      position="absolute"
      top={60}
      left={0}
      width={100}
      height="auto"
      backgroundColor="$gray2Light"
      zIndex={0} // Make sure it's below other elements
    >
      <Text fontSize={10}>GUESSES: {status?.guesses.length}</Text>
    </Stack>
  );  

  return (
    <Theme name="light">
    <YStack f={1} bg="$background">
      {/* TitleBar at the top */}
      <Stack position="absolute" top={0} left={0} right={0} bg="$background" zIndex={1}>
        {status && <TitleBar clueWord={status.clueWord} onInfoPress={handleInfoPress} />}
      </Stack>

      <Drawer visible={drawerVisible} onClose={handleDrawerClose} />

      <ScoringPanel />

      {/* GuessRows in the middle and scrollable */}
      <ScrollView
        flex={1}
        contentContainerStyle={{
          flexGrow: 1,
          justifyContent: 'flex-end',
          paddingBottom: 240,
        }}
        zIndex={0}
        width="100%"
        backgroundColor="$green4Light"
      >
        {renderGuessRows()}
      </ScrollView>

      {/* NextGuess and Keyboard at the bottom */}
      <YStack
        position="absolute"
        bottom={0}
        left={0}
        right={0}
        height={240}
        bg="$background"
        backgroundColor="$gray4Light"
        zIndex={1}
      >
        {status && (
          <>
            <YStack flexGrow={1}>
              <GuessRow
                guess={currentGuess}
                style={{ display: 'flex' }}
                isSolved={status.state == GameState.Solved}
                isEditable={true}
                editableIndex={editableIndex}
                onSquareSelect={handleSquareSelected}
                squareDim={squareWidth}
              />
              {renderError()}
              <Keyboard 
                ref={keyboardRef}              
                layout={keyboardLayout}
                onKeyPress={handleKeyPress}
              />
            </YStack>
          </>
        )}
      </YStack>
    </YStack>
    </Theme>
  )
}
