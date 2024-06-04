import React from 'react';
import { XStack, YStack, Button, Text } from 'tamagui';

interface KeyboardProps {
  onKeyPress: (key: string) => void; 
}

const Keyboard: React.FC<KeyboardProps> = ({ onKeyPress }) => {
  const backspace = "\u232B";
  const rows = [
    ['Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P'],
    ['A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L'],
    ['ENTER', 'Z', 'X', 'C', 'V', 'B', 'N', 'M', backspace]
  ];

  const Key: React.FC<{ value: string }> = ({ value }) => (
    <Button
      onPress={() => onKeyPress(value)}
      style={{
        width: value === 'ENTER' || value === backspace ? 50 : 31,
        height: 55,
        padding: 0,
        marginHorizontal: 0.5,
        borderRadius: 4,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#ddd'
      }}
    >
      <Text color='black' style={{ fontSize: value === 'ENTER' ? 12 : 16 }}>{value}</Text>
    </Button>
  );

  return (
    <YStack alignItems="center" gap={5} style={{ marginTop: 8 }}>
      {rows.map((row, index) => (
        <XStack key={index} gap={5}>
          {row.map(letter => <Key key={letter} value={letter} />)}
        </XStack>
      ))}
    </YStack>
  );
};

export default Keyboard;
