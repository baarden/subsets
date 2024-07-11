import { useEffect, useState, useImperativeHandle, forwardRef } from 'react';
import { Stack, XStack, YStack, Button, Text } from 'tamagui';
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

const Keyboard = forwardRef<KeyboardHandles, KeyboardProps>(({ layout, refWord, onKeyPress }, ref) => {
  const [keyStates, setKeyStates] = useState<{ [keyIdentifier: string]: KeyStatus }>({});

  useEffect(() => {
    console.log("layout:", layout);
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

  const getKeyColor = (status: KeyStatus | undefined) : string => {
    if (status === undefined) { return ""; }
    if (status.active === false) {
      return "#ccc";
    }
    if (refWord.length === 8 || status.highlighted === false) {
      return "#ddd";
    }
    return "#ffb";
  }

  useImperativeHandle(ref, () => ({
    enableKey,
  }));

  return (
    <YStack alignItems="center" gap={5} style={{ marginTop: 8 }}>
      {layout.map((row, rowIndex) => (
        <XStack key={"keyrow" + rowIndex} gap={5}>
          {row.map((letter, columnIndex) => {
            const keyIdentifier = `key${letter}_${rowIndex}_${columnIndex}`;
            if (!(keyIdentifier in keyStates)) { return; }
            return (
              <Button
                key={keyIdentifier}
                onPress={() => handleKeyPress(keyIdentifier, letter)}
                style={{
                  width: rowIndex == 1 ? 75 : 40,
                  height: 55,
                  padding: 0,
                  marginHorizontal: 0.5,
                  borderRadius: 4,
                  justifyContent: 'center',
                  alignItems: 'center',
                  backgroundColor: getKeyColor(keyStates[keyIdentifier]),
                }}
              >
                {keyStates[keyIdentifier].active || !keyIdentifier.startsWith("keyENTER") ? (
                  <Text color='black' style={{ fontSize: letter === 'ENTER' ? 12 : 16 }}>
                    {letter}
                  </Text>
                ) : (
                  <Stack marginTop={3} alignSelf='center'>
                    <LottieView
                      source={require("../assets/loading_dots.json")}
                      autoPlay
                      loop
                    />
                  </Stack>
                )}
              </Button>
            );
          })}
        </XStack>
      ))}
    </YStack>
  );
});

export default Keyboard;
