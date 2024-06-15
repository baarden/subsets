import { useState } from 'react'
import { useWindowDimensions } from 'react-native'
import { XStack, Stack } from 'tamagui'
import Square from './square'
import { Guess, Clue } from '../../types'

interface GuessRowProps {
  guess: Guess
  style?: any
  isSolved: boolean
  isEditable: boolean
  editableIndex?: number
  squareDim: number
  onPress?: () => void
  onSquareSelect?: (index: number) => void
}

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  style,
  isSolved,
  isEditable,
  editableIndex = null,
  squareDim,
  onPress = null,
  onSquareSelect = null,
}) => {
  const { width: screenWidth } = useWindowDimensions()
  const leftPad = screenWidth / 2 - (guess.offset + 0.5) * squareDim

  const handleSquarePress = (index: number) => {
    if (onSquareSelect != null) {
      onSquareSelect(index)
    }
  }

  return (
    <Stack
      flexDirection="row"
      style={{
        position: 'relative',
        marginLeft: leftPad,
        ...style,
      }}
      onPress={onPress || undefined}
    >
      {guess.characters.map((clue: Clue, index: number) => (
        <Square
          key={index}
          letter={clue.letter}
          clueType={clue.clueType}
          dimension={squareDim}
          isAnagram={isSolved && (guess.wordIndex === 7 || index === guess.offset - 1)}
          isEditable={isEditable && index === editableIndex}
          onPress={() => handleSquarePress(index)}
        />
      ))}
    </Stack>
  )
}

export default GuessRow
