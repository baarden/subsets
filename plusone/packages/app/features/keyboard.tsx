import { useEffect, useState, useImperativeHandle, forwardRef } from 'react';
import { Stack, XStack, YStack, Button, Text } from 'tamagui';
import LottieView from 'lottie-react-native';
import { Clue } from 'app/types/'

interface KeyboardProps {
  layout: string[][];
  refWord: string;
  onKeyPress: (key: string) => Promise<boolean>; 
}

interface KeyStatus {
  active: boolean;
  highlighted: boolean;
}

export interface KeyboardHandles {
  enableKey: (keyLabel: string, newCharacters: Clue[]) => void;
}

const backspace = '\u232B'
const buttonHeight = 55

const Keyboard = forwardRef<KeyboardHandles, KeyboardProps>(({ layout, refWord, onKeyPress }, ref) => {
  const [keyStates, setKeyStates] = useState<{ [keyIdentifier: string]: KeyStatus }>({});

  useEffect(() => {
    const states: { [key: string]: KeyStatus } =
      layout.flatMap((row, rowIndex) =>
        row.map((key, columnIndex) => `key${key}_${rowIndex}_${columnIndex}`)
      ).reduce((acc, keyIdentifier) => ({
        ...acc,
        [keyIdentifier]: { active: true, highlighted: false }
      }), {});
    const keys: [string, KeyStatus][] = Object.entries(states);
    refWord.split('').map((key) => {
      const entryMatch = keys.find(([k, v]) => k.startsWith(`key${key}_`) && v.highlighted === false)
      if (entryMatch) {
        states[entryMatch[0]].highlighted = true
      }
    });
    setKeyStates(states);
  }, [layout, refWord]);

  const handleKeyPress = (keyIdentifier: string, label: string) => {
    //if (keyStates[keyIdentifier].active === false) { return; }
    if (!keyIdentifier.startsWith(`key${backspace}`)) {
      let states = {...keyStates};
      if (states[keyIdentifier].active === false) {
        const entries: [string, KeyStatus][] = Object.entries(states);
        const key = entries.find(([k, v]) => k.startsWith(`key${label}_`) && v.active === true);
        if (key !== undefined) {
          keyIdentifier = key[0];
        }
      }
      states[keyIdentifier].active = false;
      setKeyStates(states);
    }
    onKeyPress(label);
  };

  const enableKey = async (keyLabel: string, newCharacters: Clue[]) => {
    const keyEntries: [string, KeyStatus][] = Object.entries(keyStates);
    const matchingKeys = keyEntries.filter(([key, status]) => key.startsWith(`key${keyLabel}_`) && !status.active);
    const matchingChars = newCharacters.filter((clue) => clue.letter.toUpperCase() === keyLabel);
    if (matchingKeys.length > 1 || (matchingKeys.length == 1 && matchingChars.length === 0)) {
      const lastKey = matchingKeys.splice(-1, 1)[0];
      const keyIdentifier = lastKey[0];
      let states = {...keyStates};
      states[keyIdentifier].active = true;
      setKeyStates(states);
    }
  };

  useImperativeHandle(ref, () => ({
    enableKey,
  }));

  const buttonContents = (keyIdentifier: string, letter: string, rowIndex: number) => {
    const status: KeyStatus = keyStates[keyIdentifier];
    if (status.active || !keyIdentifier.startsWith("keyENTER")) {
      return (
        <>
          { !status.highlighted && rowIndex === 0 && status.active &&
            <Text
              fontWeight={900}
              fontSize={18}
              position='absolute'
              top={-1}
              color="$blue7Light"
              userSelect='none'
            >
              +
            </Text>
          }
          <Text color='black' style={{ fontSize: letter === 'ENTER' ? 12 : 16 }} position='absolute' paddingTop={5} userSelect='none'>
            {letter}
          </Text>
        </>
      )
    }
    return (
      <Stack marginTop={3} alignSelf='center'>
        <LottieView
          source={require("../assets/loading_dots.json")}
          autoPlay
          loop
        />
      </Stack>
    )
  };

  return (
    <YStack alignItems="center" gap={5} style={{ marginTop: 8 }}>
      {layout.map((row, rowIndex) => {
        const stackKey = "keyrow" + rowIndex;
        return (
          <XStack key={stackKey} gap={5}>
            {row.map((letter, columnIndex) => {
              const keyIdentifier = `key${letter}_${rowIndex}_${columnIndex}`;
              if (!(keyIdentifier in keyStates)) { return; }
              const status = keyStates[keyIdentifier];
              const buttonWidth = rowIndex == 1 ? 75 : 40
              return (
                <Button
                  key={keyIdentifier}
                  onPress={() => handleKeyPress(keyIdentifier, letter)}
                  style={{
                    width: buttonWidth,
                    height: buttonHeight,
                    padding: 0,
                    marginHorizontal: 0.5,
                    borderRadius: 4,
                    borderColor: "$gray8Light",
                    borderWidth: 1,
                    justifyContent: 'center',
                    alignItems: 'center',
                    backgroundColor: status.active ? "$gray6Light" : "$gray8Light"
                  }}
                >
                  {buttonContents(keyIdentifier, letter, rowIndex)}
                </Button>
              );
            })}
          </XStack>
        )
      })}
    </YStack>
  );
});

Keyboard.displayName = 'Keyboard';
export default Keyboard;
