import { useState, useEffect } from 'react';
import { YStack, Text, ScrollView, Stack, View } from 'tamagui';
import TitleBar from './titlebar';
import GuessRow from './guessrow';
import Keyboard from './keyboard';
import { fetchStatus, submitGuess } from '../api';
import { GuessState, Status, Guess, GameState } from '../../types'; 

const emptyGuess: Guess = { characters: [], offset: 0, length: 0, wordIndex: 0, state: GuessState.Unsolved };
const squareWidth : number = 22;
const backspace = "\u232B";

export function GameComponent() {

  const [currentGuess, setCurrentGuess] = useState<Guess>(emptyGuess);
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState<string>('');
  const [visibleWordIndices, setVisibleWordIndices] = useState<Set<number>>(new Set<number>());

  useEffect(() => {
      fetchStatus()
      .then((statusData) => {
        updateStatus(statusData);
      })
      .catch((err) => setError('Failed to fetch game status'));
  }, []);

  const updateStatus = (statusData: Status) => {
    setStatus(statusData);
    if (!statusData) {
      setError('');
      return;
    }
    var nextGuess = statusData.nextGuess;
    setCurrentGuess(nextGuess);
    if (nextGuess.wordIndex == 7) {
      setError('Find a channel anagram matching the clue!');
      setTimeout(() => setError(''), 3000);  
    } else if (statusData.state == GameState.Solved) {
      setError('Solved!');
    } else {
      setError('');
    }
  }

  const handleKeyPress = async (key: string) => {
      if (status === null) return;
      if (key === 'ENTER') {
          if (currentGuessIndex() === status.nextGuess.characters.length) {
            try {
              await submitGuess(currentGuess.characters.map(clue => clue.letter).join(''));
              const statusData = await fetchStatus();
              updateStatus(statusData);
            } catch (err) {
              setError(err.message);
              setTimeout(() => setError(''), 3000);
            }
          } else {
              setError('Incomplete guess');
              setTimeout(() => setError(''), 2000);
          }
      } else if (key === backspace) {
        updateGuessCharacter(currentGuessIndex() - 1, ' ');
      } else if (currentGuessIndex() < status.nextGuess.length) {
        updateGuessCharacter(currentGuessIndex(), key);
      }
  };

  const currentGuessIndex = (): number => {
    var chars = currentGuess.characters;
    for (let i = 0; i < chars.length; i++) {
      if (chars[i].letter == ' ') {
        return i;
      }
    }
    return chars.length;
  };

  const updateGuessCharacter = (index: number, newLetter: string) => {
    setCurrentGuess(prev => {
      const newCharacters = [...prev.characters];
      newCharacters[index] = { ...newCharacters[index], letter: newLetter };
      return { ...prev, characters: newCharacters };
    });
  };

  const renderError = () => {
    if (!error) { return null; }
    return (
      <Stack
      alignSelf='center'
      position="absolute"
      top={0} // Adjust this value to position the message correctly
      bg="black"
      p="$2"
      zIndex={2}
      alignItems="center" // Center children horizontally
    >
          <Text color="white" fontSize="$1">
            {error}
          </Text>
    </Stack>
    );
  };

  const renderGuessRows = () => {
    if (!status) { return null; }
    return (
      <>
      <Stack
        position="absolute"
        top={0}
        bottom={0}
        left="50%"
        transform={[{ translateX: 2-((status.guesses[0].offset + 0.5) * squareWidth) }]}
        width={3 * squareWidth - 4} // Width for three columns
        backgroundColor="$blue5Light"
        zIndex={0}
      />
      {status.guesses.map((guess, index) => (
        (visibleWordIndices.has(guess.wordIndex)
        || guess.wordIndex === status.nextGuess.wordIndex
        || guess.state === GuessState.Solved) &&
        <GuessRow          
          style={{ display:
            (visibleWordIndices.has(guess.wordIndex)
            || guess.wordIndex === status.nextGuess.wordIndex
            || guess.state === GuessState.Solved) ? 'flex' : 'none' }}
          key={index}
          guess={guess}
          indent={status.indent}
          onPress={() => toggleVisibility(guess.wordIndex)}
          isSolved={status.state == GameState.Solved}
          isEditable={false}
          squareDim={squareWidth}
        />
          ))}
    </>
    );
  }

  const toggleVisibility = (wordIndex: number) => {
    console.log(`Toggles: ${Array.from(visibleWordIndices)}`);
    setVisibleWordIndices((prevIndices) => {
      const newIndices = new Set(prevIndices);
      if (newIndices.has(wordIndex)) {
        newIndices.delete(wordIndex);
      } else {
        newIndices.add(wordIndex);
      }
      return newIndices;
    });
  };

  return (
    <YStack f={1} bg="$background">
      {/* TitleBar at the top */}
      <Stack position="absolute" top={0} left={0} right={0} bg="$background" zIndex={1}>
        {status && <TitleBar clueWord={status.clueWord} />}
      </Stack>

      { /* GuessRows in the middle and scrollable */ }
      <ScrollView
        flex={1}
        contentContainerStyle={{
          flexGrow: 1,
          justifyContent: 'flex-end',
          paddingBottom: 240
        }}
        zIndex={0}
        width="100%"
        backgroundColor="$green4Light"
      >
        {renderGuessRows()}
      </ScrollView>

      {/* NextGuess and Keyboard at the bottom */}
      <YStack position="absolute" bottom={0} left={0} right={0} height={240} bg="$background" backgroundColor="$gray4Light" zIndex={1}>
        {status && (
          <>
            <YStack flexGrow={1}>
              <GuessRow guess={currentGuess} indent={status.indent} style={{ display: 'flex' }} isSolved={status.state == GameState.Solved} isEditable={true} squareDim={squareWidth} />
              {renderError()}
                <Keyboard onKeyPress={handleKeyPress} />
            </YStack>
          </>
        )}
      </YStack>
    </YStack>
  );
};

