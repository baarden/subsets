import { useEffect, useState, useImperativeHandle, forwardRef } from 'react';
import { XStack, YStack, Button, Text } from 'tamagui';

interface KeyboardProps {
  layout: string[][];
  onKeyPress: (key: string) => void; 
}

export interface KeyboardHandles {
  enableKey: (keyLabel: string) => void;
}

const backspace = '\u232B'

const Keyboard = forwardRef<KeyboardHandles, KeyboardProps>(({ layout, onKeyPress }, ref) => {
  const [keyStates, setKeyStates] = useState<{ [keyIdentifier: string]: boolean }>({});

  useEffect(() => {
    const newKeyStates = layout.flatMap((row, rowIndex) =>
      row.map((key, columnIndex) => `${key}_${rowIndex}_${columnIndex}`)
    ).reduce((acc, keyIdentifier) => ({
      ...acc,
      [keyIdentifier]: true
    }), {});

    setKeyStates(newKeyStates);
  }, [layout]);

  const handleKeyPress = (keyIdentifier: string, label: string) => {
    if (keyStates[keyIdentifier]) {
      onKeyPress(label);
      if (keyIdentifier.startsWith(backspace) || keyIdentifier.startsWith('ENTER')) { return }
      setKeyStates(prev => ({ ...prev, [keyIdentifier]: false }));
    }
  };

  const enableKey = (keyLabel: string) => {
    const keyToEnable = Object.entries(keyStates).find(([key, enabled]) => key.startsWith(keyLabel + '_') && !enabled);
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
        <XStack key={rowIndex} gap={5}>
          {row.map((letter, columnIndex) => {
            const keyIdentifier = `${letter}_${rowIndex}_${columnIndex}`;
            return (
              <Button
                key={keyIdentifier}
                onPress={() => handleKeyPress(keyIdentifier, letter)}
                style={{
                  width: rowIndex == 1 ? 50 : 31,
                  height: 55,
                  padding: 0,
                  marginHorizontal: 0.5,
                  borderRadius: 4,
                  justifyContent: 'center',
                  alignItems: 'center',
                  backgroundColor: keyStates[keyIdentifier] ? '#ddd' : '#ccc',
                }}
              >
                <Text color='black' style={{ fontSize: letter === 'ENTER' ? 12 : 16 }}>
                  {letter}
                </Text>
              </Button>
            );
          })}
        </XStack>
      ))}
    </YStack>
  );
});

export default Keyboard;
