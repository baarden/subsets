import { useEffect, useState, useImperativeHandle, forwardRef } from 'react';
import { Stack, XStack, YStack, Button, Text } from 'tamagui';
import { tokens } from '@tamagui/config/v3';
import { Variable } from '@my/ui';
import LottieView from 'lottie-react-native';

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
  enableKey: (keyLabel: string) => void;
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
    if (keyStates[keyIdentifier].active === false) { return; }
    if (!keyIdentifier.startsWith(`key${backspace}`)) {
      let states = {...keyStates};
      states[keyIdentifier].active = false;
      setKeyStates(states);
    }
    onKeyPress(label);
  };

  const enableKey = async (keyLabel: string) => {
    const keyEntries: [string, KeyStatus][] = Object.entries(keyStates)
    const keyEntry = keyEntries.find(([key, status]) => key.startsWith(`key${keyLabel}_`) && !status.active);
    if (keyEntry) {
      const keyIdentifier = keyEntry[0];
      let states = {...keyStates};
      states[keyIdentifier].active = true;
      setKeyStates(states);
    }
  };

  useImperativeHandle(ref, () => ({
    enableKey,
  }));

  const buttonContents = (keyIdentifier: string, letter: string, rowIndex: number, buttonWidth: number) => {
    const status: KeyStatus = keyStates[keyIdentifier];
    if (keyStates[keyIdentifier].active || !keyIdentifier.startsWith("keyENTER")) {
      return (
        <>
          { !status.highlighted && rowIndex === 0 && status.active &&
            <LottieView
              source={require("../assets/shimmer.json")}
              autoPlay
              width={buttonWidth}
              height={buttonHeight}
              loop
              speed={1.0 + 0.5 * (Math.random() - 0.5)}
              position='absolute'
              top={0}
              left={0}
            />
          }
          <Text color='black' style={{ fontSize: letter === 'ENTER' ? 12 : 16 }} position='absolute'>
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
      {layout.map((row, rowIndex) => (
        <XStack key={"keyrow" + rowIndex} gap={5}>
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
                  backgroundColor: status.active ? "$gray6Light" : "$gray9Light"
                }}
              >
                {buttonContents(keyIdentifier, letter, rowIndex, buttonWidth)}
              </Button>
            );
          })}
        </XStack>
      ))}
    </YStack>
  );
});

export default Keyboard;
