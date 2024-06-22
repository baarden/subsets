import { Stack } from 'tamagui'
import Square from './square'
import { Guess, Clue } from '../../types'

interface GuessRowProps {
  guess: Guess
  style?: any
  isSolved: boolean
  isEditable: boolean
  editableIndex?: number
  parentWidth: number
  showLetters: boolean
  squareDim: number
  onRowPress?: () => void
  onSquareSelect?: (index: number) => void
}

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  style,
  isSolved,
  isEditable,
  editableIndex = null,
  parentWidth,
  squareDim,
  showLetters,
  onRowPress = null,
  onSquareSelect = null,
}) => {
  const leftPad = parentWidth / 2 - (guess.offset - 0.5) * squareDim

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
      animation="medium"
      backgroundColor="$green4Light"
      onPress={onRowPress || undefined}
    >
      {guess.characters.map((clue: Clue, index: number) => (
        <Square
          key={'square' + index + '_' + guess.key}
          letter={showLetters ? clue.letter : ''}
          clueType={clue.clueType}
          dimension={squareDim}
          isAnagram={isSolved && index === guess.offset - 1}
          isEditable={isEditable && index === editableIndex}
          onPress={() => handleSquarePress(index)}
        />
      ))}
    </Stack>
  )
}

export default GuessRow
