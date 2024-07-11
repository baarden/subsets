import { useState, useEffect, useRef } from 'react'
import { ScrollView as RNScrollVIew } from 'react-native'
import { XStack, YStack, Text, ScrollView, Stack, Theme, Button } from 'tamagui'
import { Share } from '@tamagui/lucide-icons'
import TitleBar from './titlebar'
import GuessRow from './guessrow'
import Keyboard, { KeyboardHandles } from './keyboard'
import Drawer from './drawer'
import SummaryDrawer from './summary'
import { fetchStatus, submitGuess, fetchStats } from '../api'
import { Dimension, useResponsiveDimensions } from '../hooks/useResponsiveDimensions'
import { GuessState, Status, Guess, GameState, emptyGuess, ClueType, Statistics } from '../types/'

const squareWidth: number = 45
const anagramGuess: number = 7
const titleBarHeight: number = 70
const bottomPanelHeight: number = 140
const scoringPanelWidth: number = 140
const backspace = '\u232B'
const shuffle = '\uD83D\uDD00'

export function GameComponent() {
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [summaryVisible, setSummaryVisible] = useState(false);
  const screenDim: Dimension = useResponsiveDimensions();

  // Global context
  const [status, setStatus] = useState<Status | null>(null)
  const [feedback, setFeedback] = useState<string>("")
  const [statistics, setStatistics] = useState<Statistics | null>(null)
  const [referenceWord, setReferenceWord] = useState<string>("")

  // History context
  const [visibleWordIndices, setVisibleWordIndices] = useState<Set<number>>(new Set<number>())
  const [editableIndex, setEditableIndex] = useState<number>(0)

  // Guess context
  const [keyboardLayout, setKeyboardLayout] = useState<string[][]>([[]])
  const [currentGuess, setCurrentGuess] = useState<Guess>(emptyGuess)
  const [error, setError] = useState<string>('')
  const [guessCount, setGuessCount] = useState<number>(0)

  const keyboardRef = useRef<KeyboardHandles>(null);  
  const scrollViewRef = useRef<RNScrollVIew>(null);
  const showLetters = true, hideLetters = false;
  const showRows = true, hideRows = false;
  const orderByKey = true, orderByPosition = false;
  const editable = true, notEditable = false;

  const handleSquareSelected = (index: number) => {
    setEditableIndex(index)
  }

  useEffect(() => {
    const hasVisited = localStorage.getItem('hasVisited');
    if (!hasVisited) {
      setDrawerVisible(true);
      localStorage.setItem('hasVisited', 'true');
    }
    
    fetchStatus()
      .then((statusData) => {
        updateStatus(statusData)
      })
      .catch((err) => {
        setError('Failed to fetch game status');
        console.info(err);
      })
  }, [])

  useEffect(() => {
    if (scrollViewRef.current) {
      scrollViewRef.current.scrollToEnd({ animated: true });
    }
  }, [status]);

  const updateStatus = (statusData: Status) => {
    if (!statusData) {
      setError('')
      return
    }
    setStatus(statusData)
    setEditableIndex(0)

    let keys: string[][] = getKeys(statusData.guesses, statusData.nextGuess.wordIndex, statusData.state, statusData.characters)
    setKeyboardLayout(keys)
    var nextGuess = statusData.nextGuess
    setCurrentGuess(nextGuess)
    const guesses = statusData.guesses.length
    setGuessCount(guesses - 1)
    if (statusData.state == GameState.Solved) {
      displaySummary(guesses)
      const feedback = getFeedback(guesses, 7, false)
      setError(`Solved! ${feedback}`)
      return
    }
    const prevGuess = statusData.guesses.slice(-1)[0]
    if (prevGuess.state == GuessState.Solved) {
      const penultGuess = statusData.guesses.slice(-2)[0]
      const isGuessInOne = penultGuess.state == GuessState.Solved
      const feedback = getFeedback(guesses, nextGuess.wordIndex, isGuessInOne)
      if (nextGuess.wordIndex == 7) {
        setError("Congrats! Find the crosscut to solve.")
      } else if (nextGuess.wordIndex > 2) {
        setError(feedback)
      }
      setTimeout(() => setError(''), 3000)
      setVisibleWordIndices(new Set<number>())
    } else {
      setError('')
    }
    let refWord = ""
    statusData.guesses.forEach(guess => {
      if (guess.state === GuessState.Solved) {
        refWord = guess.guessWord
      }
    })
    setReferenceWord(refWord)
  }

  const displaySummary = (guesses: number) => {
    fetchStats()
      .then((statsData) => {
        setStatistics(statsData)
      })
      .catch((err) => setError('Failed to fetch statistics'))
    setFeedback(getFeedback(guesses, 7, false))
    setSummaryVisible(true)
  }

  function getFeedback(guesses: number, wordIndex: number, isGuessInOne: boolean): string {
    if (isGuessInOne && wordIndex > 2) {
      return "Guess in one!"
    }
    const ranges = [
      { min: 5, max: 10, message: "Excellent!" },
      { min: 11, max: 12, message: "Great!" },
      { min: 13, max: 14, message: "Nice!" },
      { min: 15, max: Infinity, message: "Good try!" },
    ];
    const score = guesses + 6 - wordIndex
    const feedback = ranges.find(range => score >= range.min && score <= range.max);
    return feedback ? feedback.message : "Invalid score";
  }

  const getKeys = (guesses: Guess[], wordIndex: number, gameState: GameState, keyArr: string[]): string[][] => {
    if (gameState == GameState.Solved) { return [[]] }

    const secondRow = [backspace, shuffle, 'ENTER'];
    const firstRow = keyArr.map(c => c.toUpperCase());
    return [firstRow, secondRow];
  }  

  const handleKeyPress = async (key: string): Promise<boolean> => {
    if (status === null) return false
    if (key === 'ENTER') {
      if (currentGuessLength() >= 4) {
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
        return false
      }
      keyboardRef.current?.enableKey('ENTER')
    } else if (key == shuffle) {
      let keys = [shuffleArray(keyboardLayout[0]), keyboardLayout[1]]
      setKeyboardLayout(keys)
      const newCharacters = currentGuess.characters.map(character => ({
        ...character,
        letter: ' '
      }))
      setCurrentGuess({
        ...currentGuess,
        characters: newCharacters
      })
      setEditableIndex(0)
    } else if (key === backspace) {
      let editIndex = editableIndex
      let deletedChar = currentGuess.characters[editIndex].letter
      if (deletedChar === ' ') {
        if (editIndex === 0) { return false }
        editIndex--
        setEditableIndex(editIndex)
        deletedChar = currentGuess.characters[editIndex].letter
        if (deletedChar === ' ') { return false }
      }
      updateGuessCharacter(editIndex, ' ')
      keyboardRef.current?.enableKey(deletedChar.toUpperCase())
    } else if (editableIndex >= 0) {
      let deletedChar = currentGuess.characters[editableIndex].letter
      if (deletedChar !== ' ') {
        keyboardRef.current?.enableKey(deletedChar.toUpperCase())
      }
      updateGuessCharacter(editableIndex, key)
      setEditableIndex(nextEditableIndex())
    }
    return true
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

  const handleSummaryClose = () => {
    setSummaryVisible(false);
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
        bottom={ -20 }
        backgroundColor={'black'}
        padding="$2"
        zIndex={100}
        alignItems="center"
      >
        <Text color="white" fontSize="$5" fontWeight={800}>
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
  
  const renderGuessRow = (
    guess: Guess,
    shouldInsertSpacer: boolean,
    squareDim: number,
    parentWidth: number,
    showLetters: boolean,
    keyPrefix: string,
    isEditable: boolean,
    editableIndex: number | undefined = undefined
  ) => (
    <>
      <GuessRow
        key={'guessrow_' + guess.key + '_' + keyPrefix}
        style={{ display: 'flex' }}
        guess={guess}
        onRowPress={() => toggleVisibility(guess.wordIndex)}
        isAnagramGuess={ status?.state == GameState.Solved || (status?.nextGuess.wordIndex === anagramGuess && guess.wordIndex < anagramGuess) }
        isEditable={isEditable}
        editableIndex={editableIndex}
        onSquareSelect={isEditable ? handleSquareSelected : undefined}
        parentWidth={parentWidth}
        showLetters={showLetters}
        keyPrefix={keyPrefix}
        squareDim={squareDim}
      />
      {shouldInsertSpacer && renderSpacer()}
      </>
  );
  
  const renderGuessRows = (
    squareDim: number, 
    parentWidth: number, 
    showLetters: boolean, 
    showRows: boolean,
    orderByKey: boolean,
    keyPrefix: string
  ) => {
    if (!status) {
      return null;
    }
    let guesses = status.guesses
    if (orderByKey) {
      guesses = guesses.slice().sort((a, b) => a.key - b.key);
    }
    
    return (
      <YStack marginTop={10}>
        {guesses.map((guess, _) => {
          const isVisible =
            showRows ||
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
            && showLetters
  
          return renderGuessRow(guess, shouldInsertSpacer, squareDim, parentWidth, showLetters, keyPrefix, notEditable);
        })}
        {renderGuessRow(
          currentGuess,
          false,
          squareWidth,
          screenDim.width,
          showLetters,
          "main",
          editable,
          editableIndex)}
      </YStack>
    );
  }

  const toggleVisibility = (wordIndex: number) => {
    if (status?.nextGuess.wordIndex == 7) { return }
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

  const ScoringPanel = () => {
    if (!status) {
      return null;
    }
    return (    
      <Stack
        position="absolute"
        top={60}
        right={0}
        width={scoringPanelWidth}
        padding={5}
        height="auto"
        backgroundColor="$green4Light"
        zIndex={0}
      >
        <XStack justifyContent="space-between">
          <Stack flex={1} />
          <Text fontSize={12} flex={1}>GUESSES: {(status?.guesses.length || 1) - 1}</Text>
          { status?.state == GameState.Solved && (
            <Button size="$1" icon={Share} theme="light"/>
          )}
        </XStack>
        { renderGuessRows(10, scoringPanelWidth, hideLetters, showRows, orderByKey, "score") }
      </Stack>
    );
  }

  return (
    <Theme name="light">
    <YStack backgroundColor="$gray3Light" height={screenDim.height}>
      {/* TitleBar at the top */}
      <Stack
        position="absolute"
        top={0}
        left={0}
        right={0}
        height={titleBarHeight}
        backgroundColor="$background"
        zIndex={2}
      >
        {status && <TitleBar guessCount={guessCount} onInfoPress={handleInfoPress} />}
        { renderError() }
      </Stack>

      <Drawer visible={drawerVisible} onClose={handleDrawerClose} />
      <SummaryDrawer statistics={statistics || undefined} status={status || undefined} feedback={feedback} visible={summaryVisible} onClose={handleSummaryClose} />

      {/* GuessRows in the middle and scrollable */}
      <ScrollView
        ref={scrollViewRef}      
        position="absolute"
        top={titleBarHeight}
        height={screenDim.height - titleBarHeight - (status?.state === GameState.Solved ? 0 : bottomPanelHeight) }
        paddingTop={20}
        width="100%"
        contentContainerStyle={{
          flexGrow: 1,
          justifyContent: 'flex-start'
        }}
        zIndex={1}
      >
        {
          renderGuessRows(squareWidth, screenDim.width, showLetters, hideRows, orderByPosition, "main")
        }
        { (status?.state == GameState.Unsolved && status.nextGuess.wordIndex == 7) &&
          <YStack alignItems='center' width="100%" marginTop={8}>
            <Text fontWeight='bold' fontSize={12}>CLUE: “{status?.clueWord}”</Text>
          </YStack>
        }
      </ScrollView>

      {status && (status.state !== GameState.Solved) && (
          <>
            <YStack
              position="absolute"
              bottom={0}
              left={0}
              right={0}
              height={bottomPanelHeight}
              backgroundColor="$gray4Light"
              zIndex={2}
              borderTopColor="$gray7Light"
              borderTopWidth={1}
            >
                    <Keyboard 
                      ref={keyboardRef}              
                      layout={keyboardLayout}
                      refWord={referenceWord.toUpperCase()}
                      onKeyPress={handleKeyPress}
                    />
            </YStack>
        </>
      )}

    </YStack>
    </Theme>
  )
}
