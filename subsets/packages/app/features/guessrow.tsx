import { XStack } from 'tamagui'
import { ChevronsUpDown } from '@tamagui/lucide-icons'
import Square from './square'
import { Guess, Clue, GuessState } from '../types/'

interface GuessRowProps {
  guess: Guess
  style?: any
  isAnagramGuess: boolean
  isEditable: boolean
  editableIndex?: number
  parentWidth: number
  showLetters: boolean
  hasHiddenRows: boolean
  squareDim: number
  keyPrefix: string
  onRowPress?: () => void
  onSquareSelect?: (index: number) => void
}

const extraLetterIndex = 6

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  style,
  isAnagramGuess,
  isEditable,
  editableIndex = null,
  parentWidth,
  squareDim,
  showLetters,
  hasHiddenRows,
  keyPrefix,
  onRowPress = null,
  onSquareSelect = null,
}) => {
  const extraLetter: boolean = (guess.wordIndex === extraLetterIndex)
  let offset = guess.length
  if (extraLetter) {
    offset = 1
  } else if (isAnagramGuess) {
    offset = guess.characters.findIndex(c => c.letter === guess.highlightLetter) + 1
  }
  const leftPad = parentWidth / 2 - offset * squareDim / 2;
  let hLetter : string = guess.highlightLetter;

  const handleSquarePress = (index: number) => {
    if (onSquareSelect != null) {
      onSquareSelect(index)
    }
  }

  return (
    <XStack
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
      {guess.characters.map((clue: Clue, index: number) => {
        let highlighted = false;
        if (clue.letter === hLetter && guess.state === GuessState.Solved
            && (guess.length > 3 || isAnagramGuess)) {
          highlighted = true;
          hLetter = "";
        }
        return (
          <Square
          key={keyPrefix + '_square' + index + '_' + guess.key}
          letter={showLetters ? clue.letter : ''}
          clueType={clue.clueType}
          dimension={squareDim}
          isAnagramGuess={isAnagramGuess}
          isAnagramLetter={isAnagramGuess && (highlighted || extraLetter)}
          isHighlighted={highlighted}
          isEditable={isEditable && index === editableIndex}
          onPress={() => handleSquarePress(index)}
        />
        )})
      }
      {
        guess.state === GuessState.Solved && guess.wordIndex > 1 && hasHiddenRows &&
        <ChevronsUpDown size="$1" marginTop={12} color="$gray9Light" />
      }
    </XStack>
  )
}

export default GuessRow
