import React from 'react';
import { useWindowDimensions } from 'react-native';
import { XStack, Stack } from 'tamagui';
import Square from './square';
import { Guess } from '../../types';

interface GuessRowProps {
  guess: Guess;
  indent: number;
  style?: any;
  onPress?: () => void;
  isSolved: boolean;
  isEditable: boolean;
  squareDim: number;
}

const screenWidth : number = 500;

const GuessRow: React.FC<GuessRowProps> = ({ guess, indent, style, onPress, isSolved, isEditable, squareDim }) => {
  const { width: screenWidth } = useWindowDimensions();
  const leftPad = screenWidth / 2 - (guess.offset + 0.5) * squareDim;

  return (
    <Stack
      flexDirection="row"
      style={{
        position: 'relative',
        marginLeft: leftPad
      }}
      onPress={onPress || undefined}
    >
      {guess.characters.map((clue, index) => (
            <Square key={index} letter={clue.letter} clueType={clue.clueType} dimension={squareDim} isAnagram={isSolved && (guess.wordIndex == 7 || index == guess.offset - 1) } isEditable={isEditable} />
      ))}
    </Stack>
  );
};

export default GuessRow;
