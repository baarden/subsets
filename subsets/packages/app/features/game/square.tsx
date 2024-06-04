import React from 'react';
import { Stack, Text, Variable, createTokens, getTokens } from 'tamagui';
import { tokens as baseTokens } from '@tamagui/config/v3'
import { ClueType } from '../../types';


interface SquareProps {
    letter?: string;
    clueType?: ClueType;
    dimension: number;
    isAnagram?: boolean;
    isEditable?: boolean;
}

const customTokens = createTokens({
  ...baseTokens,
  color: {
    ...baseTokens.color,
    transparent: '#00000000',
  },
});

const Square: React.FC<SquareProps> = ({ letter = ' ', clueType = ClueType.Empty, dimension, isAnagram = false, isEditable = false }) => {
  const squareMargin : number = 2;
  const squareDim : number = dimension - 2 * squareMargin;

  const getBackgroundColor = (): Variable => {
        switch (clueType) {
            case ClueType.AllCorrect:
              return (isAnagram) ? customTokens.color.blue10Light : customTokens.color.blue8Light;
            case ClueType.CorrectLetter:
              return customTokens.color.orange8Light;
            case ClueType.Incorrect:
              return customTokens.color.gray8Light;
            default:
              return customTokens.color.transparent;
        }
    };

    return (
        <Stack
            width={squareDim}
            height={squareDim + 5}
            justifyContent="center"
            alignItems="center"
            backgroundColor={getBackgroundColor()}
            borderColor={isEditable ? 'blue' : 'black'}
            borderWidth={isEditable ? 2 : 1}
            margin={squareMargin}
            cursor={isEditable ? 'pointer' : 'default'}
        >
            <Text color={'black'} fontSize={15} fontWeight="bold">
                {letter.toUpperCase()}
            </Text>
        </Stack>
    );
};

export default Square;