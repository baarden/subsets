import { useEffect, useState, useImperativeHandle, forwardRef } from 'react';
import { Stack, XStack, YStack, Button, Text } from 'tamagui';
import LottieView from 'lottie-react-native';

interface KeyboardProps {
  layout: string[][];
  onKeyPress: (key: string) => Promise<boolean>; 
}

export interface KeyboardHandles {
  enableKey: (keyLabel: string) => void;
}

const backspace = '\u232B'

const Keyboard = forwardRef<KeyboardHandles, KeyboardProps>(({ layout, onKeyPress }, ref) => {
  const [keyStates, setKeyStates] = useState<{ [keyIdentifier: string]: boolean }>({});

  useEffect(() => {
    const newKeyStates = layout.flatMap((row, rowIndex) =>
      row.map((key, columnIndex) => `key${key}_${rowIndex}_${columnIndex}`)
    ).reduce((acc, keyIdentifier) => ({
      ...acc,
      [keyIdentifier]: true
    }), {});

    setKeyStates(newKeyStates);
  }, [layout]);

  const handleKeyPress = async (keyIdentifier: string, label: string) => {
    if (keyStates[keyIdentifier]) {
      const success: boolean = await onKeyPress(label);
      if (keyIdentifier.startsWith(`key${backspace}`) || !success) { return }
      setKeyStates(prev => ({ ...prev, [keyIdentifier]: false }));
    }
  };

  const enableKey = (keyLabel: string) => {
    const keyToEnable = Object.entries(keyStates).find(([key, enabled]) => key.startsWith('key' + keyLabel + '_') && !enabled);
    console.log(keyStates);
    if (keyToEnable) {
      const [keyIdentifier,] = keyToEnable;
      setKeyStates(prev => ({ ...prev, [keyIdentifier]: true }));
    }
  };

  useImperativeHandle(ref, () => ({
    enableKey,
  }));

  return (
    <YStack alignItems="center" gap={5} style={{ marginTop: 8 }}>
      {layout.map((row, rowIndex) => (
        <XStack key={"keyrow" + rowIndex} gap={5}>
          {row.map((letter, columnIndex) => {
            const keyIdentifier = `key${letter}_${rowIndex}_${columnIndex}`;
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
                  backgroundColor: keyStates[keyIdentifier] ? '#ddd' : '#ccc',
                }}
              >
                {keyStates[keyIdentifier] || !keyIdentifier.startsWith("keyENTER") ? (
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
