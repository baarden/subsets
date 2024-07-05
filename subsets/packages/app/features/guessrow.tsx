import { Stack } from 'tamagui'
import Square from './square'
import { Guess, Clue } from '../types/'

interface GuessRowProps {
  guess: Guess
  style?: any
  isAnagramGuess: boolean
  isEditable: boolean
  editableIndex?: number
  parentWidth: number
  showLetters: boolean
  squareDim: number
  keyPrefix: string
  onRowPress?: () => void
  onSquareSelect?: (index: number) => void
}

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  style,
  isAnagramGuess,
  isEditable,
  editableIndex = null,
  parentWidth,
  squareDim,
  showLetters,
  keyPrefix,
  onRowPress = null,
  onSquareSelect = null,
}) => {
  const offset = isAnagramGuess ? guess.offset : guess.length;
  const leftPad = parentWidth / 2 - offset * squareDim / 2;

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
      backgroundColor="$gray3Light"
      onPress={onRowPress || undefined}
    >
      {guess.characters.map((clue: Clue, index: number) => (
        <Square
          key={keyPrefix + '_square' + index + '_' + guess.key}
          letter={showLetters ? clue.letter : ''}
          clueType={clue.clueType}
          dimension={squareDim}
          isAnagramGuess={isAnagramGuess}
          isAnagramLetter={isAnagramGuess && index === guess.offset - 1}
          isEditable={isEditable && index === editableIndex}
          onPress={() => handleSquarePress(index)}
        />
      ))}
    </Stack>
  )
}

export default GuessRow
