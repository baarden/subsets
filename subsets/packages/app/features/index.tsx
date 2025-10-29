import { useState, useEffect, useRef } from 'react'
import { ScrollView as RNScrollVIew } from 'react-native'
import { YStack, Text, ScrollView, Stack, Theme } from 'tamagui'
import AsyncStorage from '@react-native-async-storage/async-storage';
import TitleBar from 'app/features/titlebar'
import GuessRow from 'app/features/guessrow'
import EditRow from 'app/features/editrow'
import Keyboard, { KeyboardHandles } from 'app/features/keyboard'
import Drawer from 'app/features/drawer'
import SummaryDrawer from 'app/features/summary'
import { fetchStatus, submitGuess, fetchStats, ConflictError } from 'app/api'
import { Dimension, useResponsiveDimensions } from 'app/hooks/useResponsiveDimensions'
import {
  GuessState, Status, Guess, GameState, emptyGuess, Clue, ClueType, Letter,
  Statistics, GameSettings, ScoringRange, ExampleText, KeyboardAction
} from 'app/types/'

const squareWidth = 45
const titleBarHeight = 70
const bottomPanelHeight = 140
const backspace = '\u232B'
const shuffle = '\uD83D\uDD00'

const plusOneMoreScoreRanges: ScoringRange[] = [
  { min: 5, max: 5, message: "Wow, perfect!" },
  { min: 6, max: 10, message: "Excellent job!" },
  { min: 11, max: 12, message: "Great job!" },
  { min: 13, max: 14, message: "Nice job!" },
  { min: 15, max: Infinity, message: "Good try!" },
];

const plusOneMoreExamples: ExampleText = {
  startWord: "MET",
  exampleWord: "MANY",
  correctLetters: "M",
  wrongLetters: "A, N",
  nonLetters: "Y",
  anagram: "CYANIDE"
}

const plusOneSettings: GameSettings = {
  siteUrl: "https://plusone.ngrok.app",
  sitePath: "",
  anagramIndex: 6,
  apiPath: "/api",
  gameName: "Plus One",
  scoreRanges: plusOneMoreScoreRanges,
  exampleText: plusOneMoreExamples,
  logoImagePath: "/plus-one.png",
  fullExampleImagePath: "/full_example.png",
  exampleImagePath: "/example.png",
  anagramImagePath: "/anagram.png"
}

const plusOneMoreSettings: GameSettings = {
  siteUrl: "https://plusone.ngrok.app",
  sitePath: "/more",
  anagramIndex: 7,
  apiPath: "/api/more",
  gameName: "Plus One More",
  scoreRanges: plusOneMoreScoreRanges,
  exampleText: plusOneMoreExamples,
  logoImagePath: "/plus-one-more.png",
  fullExampleImagePath: "/full_example.png",
  exampleImagePath: "/example.png",
  anagramImagePath: "/anagram.png"
}

interface GameComponentProps {
  path: string;
}

export const GameComponent: React.FC<GameComponentProps> = ({ path }) => {
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
  const [swapState, setSwapState] = useState<boolean>(false)
  const [editRowChars, setEditRowChars] = useState<string[]>([]);
  const [editRowPlusOneChars, setEditRowPlusOneChars] = useState<string[]>([]);

  const config = (path === "/more") ? plusOneMoreSettings : plusOneSettings;
  const extraLetterIndex: number = config.anagramIndex - 1
  const keyboardRef = useRef<KeyboardHandles>(null);
  const scrollViewRef = useRef<RNScrollVIew>(null);
  const hideRows = false;
  const orderByPosition = false;
  const editable = true;
  const notEditable = false;
  const notSwapState = false;
  var render = useRef(0);
  render.current += 1;

  useEffect(() => {
    checkFirstVisit();

    fetchStatus(config)
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

  const checkFirstVisit = async () => {
    const hasVisited = await AsyncStorage.getItem('hasVisited');
    if (!hasVisited) {
      setDrawerVisible(true);
      await AsyncStorage.setItem('hasVisited', 'true');
    }
  };

  const updateStatus = (statusData: Status) => {
    if (!statusData) {
      setError('')
      return
    }
    setStatus(statusData)
    setEditableIndex(0)

    const prevGuess = statusData.guesses.slice(-1)[0]
    const nextGuess = initializeCurrentGuess(statusData.nextGuess, statusData.guesses, statusData.characters);
    const extraGuesses = (statusData.state === GameState.Solved || statusData.nextGuess.wordIndex === config.anagramIndex) ? 2 : 1;
    const guesses = statusData.guesses.length - extraGuesses
    setGuessCount(guesses)
    if (statusData.state == GameState.Solved) {
      displaySummary(guesses)
      const feedback = getFeedback(guesses, config.anagramIndex, false)
      setError(`You solved it in ${guesses}! ${feedback}`)
      return
    }
    if (prevGuess.state == GuessState.Solved) {
      const penultGuess = statusData.guesses.slice(-2)[0]
      const isGuessInOne = penultGuess.state == GuessState.Solved
      const feedback = getFeedback(guesses, nextGuess.wordIndex, isGuessInOne)
      if (nextGuess.wordIndex == config.anagramIndex) {
        setError("Arrange the plus-one letters to solve")
      } else if (nextGuess.wordIndex > 2) {
        setError(feedback)
      }
      setTimeout(() => setError(''), 3000)
      setVisibleWordIndices(new Set<number>())
    } else {
      setError('')
    }
    let refWord = ""
    if (statusData.nextGuess.wordIndex < config.anagramIndex) {
      statusData.guesses.forEach(guess => {
        if (guess.state === GuessState.Solved) {
          refWord = guess.guessWord
        }
      })
    }
    setReferenceWord(refWord)
    let keys: string[][] = getKeys(
      statusData.guesses,
      statusData.nextGuess.wordIndex,
      statusData.state,
      statusData.characters,
      refWord
    )
    setKeyboardLayout(keys)
  }

  const initializeCurrentGuess = (nextGuess: Guess, guesses: Guess[], chars: string[]): Guess => {
    const refWord: Guess | undefined = guesses.find(g => g.wordIndex === nextGuess.wordIndex - 1 && g.state === GuessState.Solved);
    if (refWord === undefined) { return nextGuess; }
    const refChars = refWord.guessWord.split('').sort();
    let plusOneChars = [...chars];
    refChars.forEach(c => {
      const idx = plusOneChars.indexOf(c);
      if (idx !== -1) {
        plusOneChars.splice(idx, 1);
      }
    });
    setEditRowChars(refChars);
    setEditRowPlusOneChars(plusOneChars);

    let newCharacters: Clue[] = refChars.map(c => {
      return {letter: c, clueType: ClueType.Empty};
    })
    newCharacters.push({letter: "", clueType: ClueType.Empty});
    const guess = { 
      ...nextGuess,
      characters: newCharacters
    };
    setCurrentGuess(guess);

    return guess;
  }

  const displaySummary = (guesses: number) => {
    fetchStats(config)
      .then((statsData) => {
        setStatistics(statsData)
      })
      .catch(() => setError('Failed to fetch statistics'))
    setFeedback(getFeedback(guesses, config.anagramIndex, false))
    setTimeout(() => setSummaryVisible(true), 500)
  }

  function getFeedback(guesses: number, wordIndex: number, isGuessInOne: boolean): string {
    if (isGuessInOne && wordIndex > 2) {
      return "Woohoo, guess in one!"
    }
    const score = guesses + config.anagramIndex - wordIndex
    const feedback = config.scoreRanges.find(range => score >= range.min && score <= range.max);
    return feedback ? feedback.message : "Invalid score";
  }

  const getKeys = (
    guesses: Guess[],
    wordIndex: number,
    gameState: GameState,
    keyArr: string[],
    refWord: string
  ): string[][] => {
    if (gameState == GameState.Solved) { return [[]] }

    let firstRow = keyArr.map(c => c.toUpperCase());
    const secondRow = [backspace, shuffle, 'ENTER'];

    if (wordIndex < config.anagramIndex) {
      firstRow = removeKeysFromClues(firstRow, wordIndex, refWord, guesses);
    }

    return [firstRow, secondRow];
  }

  const removeKeysFromClues = (firstRow: string[], wordIndex: number, refWord: string, guesses: Guess[]): string[] => {
    // firstRow -> { key: string, status: number, plusone: boolean }
    const goodTypes = [ClueType.AllCorrect, ClueType.CorrectLetter];
    let states = firstRow.map(c => { return { key: c, state: KeyboardAction.Unset, plusone: true }; });
    refWord.toUpperCase().split('').forEach(c => {
      const idx = states.findIndex(s => s.key === c && s.plusone === true);
      states[idx].plusone = false;
    });
    const stateRef = states.map(s => {return {...s};});
    // Loop over guesses for the current wordIndex
    guesses.forEach((guess) => {
      if (guess.wordIndex !== wordIndex) { return; }
      const chars = guess.characters;
      let localStates = stateRef.map(s => {return {...s};});
      let goodCount = 0;
      let goodPlusOne = false;
      chars.forEach(c => {
        const upperC = c.letter.toUpperCase();
        // For any good clue:
        // - set an unmarked char to safe. If none, set a deleted char to safe.
        // - if that char is plus-one, set goodPlusOne to true        
        if (goodTypes.includes(c.clueType)) {
          let idx = localStates.findIndex(s => s.key === upperC && s.state === KeyboardAction.Unset);
          if (idx === -1) {
            idx = localStates.findIndex(s => s.key === upperC && s.state === KeyboardAction.Delete)
          }
          localStates[idx].state = KeyboardAction.Keep;
          if (localStates[idx].plusone) {
            goodPlusOne = true;
          }
          goodCount++;
          // For any incorrect clue, set an unmarked char as deleted. Prefer to remove plus-one chars first.
        } else if (c.clueType === ClueType.Incorrect) {
          const idx = localStates.findLastIndex(s => s.key === upperC && s.state === KeyboardAction.Unset);
          if (idx >= 0) {
            localStates[idx].state = KeyboardAction.Delete;
          }
        }
      });
      // Merge with master: keep > delete > unset
      for (let i = 0; i < states.length; i++) {
        const ls = localStates[i];
        const s = states[i];
        // If all clues are good, mark any unmarked chars as deleted
        if (ls.state === 0 && goodCount === guess.length) {
          localStates[i].state = KeyboardAction.Delete;
        }
        // If goodPlusOne is true, mark any unmarked plus-one as deleted
        if (ls.plusone && goodPlusOne && ls.state !== KeyboardAction.Keep) {
          localStates[i].state = KeyboardAction.Delete;
        }
        const newState = (ls.state === KeyboardAction.Keep || s.state === KeyboardAction.Keep)
          ? KeyboardAction.Keep : (ls.state === KeyboardAction.Delete || s.state === KeyboardAction.Delete)
            ? KeyboardAction.Delete : KeyboardAction.Unset;
        s.state = newState;
      }
    });
    // Apply any master deletions to firstRow
    for (let i = states.length - 1; i >= 0; i--) {
      const s = states[i];
      if (s.state === KeyboardAction.Delete) {
        firstRow.splice(i, 1);
      }
    }
    return firstRow;
  }

  const handleSquareSelected = (index: number) => {
    if (index === editableIndex) {
      setSwapState(!swapState)
      return
    }
    if (swapState) {
      let chars = [...currentGuess.characters];
      const originalIndexLetter = chars[index].letter
      chars[index].letter = chars[editableIndex].letter
      chars[editableIndex].letter = originalIndexLetter
      const guess = {
        ...currentGuess,
        characters: chars
      }
      setCurrentGuess(guess)
      setSwapState(false)
    }
    setEditableIndex(index)
  }

  const handleKeyPress = async (key: string): Promise<boolean> => {
    if (status === null) return false
    setSwapState(false)
    if (key === 'ENTER') {
      if (currentGuessLength() >= 4) {
        try {
          const guess = currentGuess.characters.map((clue) => clue.letter).join('')
          await submitGuess(guess, status.today, config)
          const statusData = await fetchStatus(config)
          updateStatus(statusData)
        } catch (err) {
          if (err instanceof ConflictError) {
            const statusData = await fetchStatus(config)
            updateStatus(statusData)
          } else {
            setError(err.message)
            setTimeout(() => setError(''), 3000)
          }
        }
      } else {
        setError('Incomplete guess')
        setTimeout(() => setError(''), 2000)
      }
      keyboardRef.current?.enableKey('ENTER', currentGuess.characters)
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
      const newCharacters = updateGuessCharacter(editIndex, ' ')
      keyboardRef.current?.enableKey(deletedChar.toUpperCase(), newCharacters)
    } else if (editableIndex >= 0) {
      const deletedChar = currentGuess.characters[editableIndex].letter
      const newCharacters = updateGuessCharacter(editableIndex, key)
      if (deletedChar !== ' ') {
        keyboardRef.current?.enableKey(deletedChar.toUpperCase(), newCharacters)
      }
      setEditableIndex(nextEditableIndex())
    }
    return true
  }

  const updateGuessCharacter = (index: number, newLetter: string): Clue[] => {
    let newCharacters: Clue[] = [...currentGuess.characters]
    newCharacters[index] = { ...newCharacters[index], letter: newLetter }
    const newGuess = { ...currentGuess, characters: newCharacters }
    setCurrentGuess(newGuess)
    return newCharacters
  }

  function shuffleArray<T>(array: T[]): T[] {
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
        bottom={-20}
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

  const handleMoveSquare = (oldIndex: number, newIndex: number) => {
    let chars: Clue[] = [...currentGuess.characters];
    const insIndex = (oldIndex > newIndex) ? newIndex : newIndex + 1;
    chars.splice(insIndex, 0, currentGuess.characters[oldIndex]);
    const delIndex = (newIndex > oldIndex) ? oldIndex : oldIndex + 1;
    chars.splice(delIndex, 1);
    const newGuess: Guess = {
      ...currentGuess,
      characters: chars
    }
    setCurrentGuess(newGuess);
  };

  interface RenderGuessRowArgs {
    guess: Guess;
    key: string;
    squareDim: number;
    parentWidth: number;
    hasHiddenRows: boolean;
    keyPrefix: string;
    isVisible: boolean;
  }

  const renderGuessRow = (
    { guess, key, squareDim, parentWidth, hasHiddenRows, keyPrefix, isVisible }: RenderGuessRowArgs
  ) => {
    return (
      <GuessRow
        key={key}
        guess={guess}
        isVisible={isVisible}
        parentWidth={parentWidth}
        hasHiddenRows={hasHiddenRows}
        keyPrefix={keyPrefix}
        squareDim={squareDim}
        onRowPress={() => toggleVisibility(guess.wordIndex)}
        config={config}
      />
    );
  };

  const renderGuessRows = (
    squareDim: number,
    parentWidth: number,
    showRows: boolean,
    orderByKey: boolean,
    keyPrefix: string
  ) => {
    if (!status) {
      return null;
    }
    let guesses = status.guesses;
    let curWordIndex = 0;
    let guessCount = 0;
    let hasHiddenRows = false;
    if (orderByKey) {
      guesses = guesses.slice().sort((a, b) => a.key - b.key);
    }

    return (
      <YStack marginTop={10} flexDirection='column'>
        {guesses.map((guess) => {
          if (guess.wordIndex !== curWordIndex) {
            guessCount = 0;
            curWordIndex = guess.wordIndex;
            hasHiddenRows = false;
          }
          guessCount++;
          if (guessCount > 1) {
            hasHiddenRows = true;
          }

          let isVisible =
            showRows ||
            visibleWordIndices.has(guess.wordIndex) ||
            guess.wordIndex === status.nextGuess.wordIndex ||
            guess.state === GuessState.Solved;
          if (status.state === GameState.Solved && guess.wordIndex === config.anagramIndex) {
            isVisible = false;
          }
          const key = 'guessrow_' + guess.key + '_' + keyPrefix;

          return renderGuessRow(
            {
              guess: guess,
              key: key,
              squareDim: squareDim,
              parentWidth: parentWidth,
              hasHiddenRows: hasHiddenRows,
              keyPrefix: keyPrefix,
              isVisible: isVisible
            });
        })}
        {currentGuess.wordIndex === config.anagramIndex && renderSpacer()}
      </YStack>
    );
  }

  const toggleVisibility = (wordIndex: number) => {
    if (status?.nextGuess.wordIndex == config.anagramIndex) { return }
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

  return (
    <Theme name="light">
      <YStack backgroundColor="$gray3Light" height={screenDim.height}>
        <Stack
          position="absolute"
          top={0}
          left={0}
          right={0}
          height={titleBarHeight}
          backgroundColor="$background"
          zIndex={2}
        >
          {status && <TitleBar guessCount={guessCount} onInfoPress={handleInfoPress} config={config} />}
          {renderError()}
        </Stack>

        <Drawer visible={drawerVisible} onClose={handleDrawerClose} config={config} />
        <SummaryDrawer
          statistics={statistics || undefined}
          status={status || undefined}
          feedback={feedback}
          visible={summaryVisible}
          onClose={handleSummaryClose}
          config={config} />

        <ScrollView
          ref={scrollViewRef}
          position="absolute"
          top={titleBarHeight}
          height={screenDim.height - titleBarHeight - (status?.state === GameState.Solved ? 0 : bottomPanelHeight)}
          paddingTop={20}
          width="100%"
          contentContainerStyle={{
            flexGrow: 1,
            justifyContent: 'flex-start'
          }}
          zIndex={1}
        >
          {
            renderGuessRows(squareWidth, screenDim.width, hideRows, orderByPosition, "main")
          }
          <EditRow
            clues={editRowChars}
            plusOnes={editRowPlusOneChars}
            parentWidth={screenDim.width}
            isAnagramGuess={status?.state == GameState.Solved || (status?.nextGuess.wordIndex === config.anagramIndex && currentGuess.wordIndex < config.anagramIndex)}
            keyPrefix="main"
            squareDim={squareWidth}
            config={config}
            onMoveSquare={handleMoveSquare}
          />
          {(status?.state == GameState.Unsolved && status.nextGuess.wordIndex == config.anagramIndex) &&
            <YStack alignItems='center' width="100%" marginVertical={8}>
              <Text fontWeight='bold' fontSize={12}>CLUE: “{status.clueWord}”</Text>
            </YStack>
          }
        </ScrollView>

        {status && (status.state !== GameState.Solved) &&
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
        }

      </YStack>
    </Theme>
  )
}
