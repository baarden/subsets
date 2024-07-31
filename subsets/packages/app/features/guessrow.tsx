import { XStack } from 'tamagui'
import { ChevronsUpDown } from '@tamagui/lucide-icons'
import Square from 'app/features/square'
import { Guess, Clue, GuessState, GameSettings } from 'app/types/'

interface GuessRowProps {
  guess: Guess
  isAnagramGuess: boolean
  isEditable: boolean
  isSwapState: boolean
  editableIndex?: number
  parentWidth: number
  showLetters: boolean
  hasHiddenRows: boolean
  squareDim: number
  keyPrefix: string
  config: GameSettings
  onRowPress?: () => void
  onSquareSelect?: (index: number) => void
  animation: string
}

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  isAnagramGuess,
  isEditable,
  isSwapState,
  editableIndex = null,
  parentWidth,
  squareDim,
  showLetters,
  hasHiddenRows,
  keyPrefix,
  config,
  onRowPress = null,
  onSquareSelect = null,
}) => {
  const extraLetterIndex = config.anagramIndex - 1
  const extraLetter: boolean = (guess.wordIndex === extraLetterIndex)
  let offset = guess.length
  if (extraLetter) {
    offset = 1
  } else if (isAnagramGuess) {
    if (guess.state === GuessState.Solved) {
      offset = guess.characters.findIndex(c => c.letter === guess.highlightLetter) + 1
    } else {
      offset = guess.length / 2
    }
  }
  const leftPad = parentWidth / 2 - offset * squareDim / 2;
  let hLetter : string = guess.highlightLetter;

  const handleSquarePress = (index: number) => {
    if (onSquareSelect != null) {
      onSquareSelect(index);
    }
  }
  return (
    <XStack
      flexDirection="row"
      style={{
        position: 'relative',
        marginLeft: leftPad,
        display: 'flex'
      }}
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
        const key = keyPrefix + '_square' + index + '_' + guess.key
        return (
          <Square
            key={key}
            letter={showLetters ? clue.letter : ''}
            clueType={clue.clueType}
            dimension={squareDim}
            isAnagramGuess={isAnagramGuess}
            isAnagramLetter={isAnagramGuess && (highlighted || extraLetter)}
            isHighlighted={highlighted}
            isEditable={isEditable && index === editableIndex}
            isSwapState={isSwapState}
            onPress={() => handleSquarePress(index)}
          />
        )})
      }
      {
        guess.state === GuessState.Solved && guess.wordIndex > 0 && hasHiddenRows &&
        <ChevronsUpDown size="$1" marginTop={12} color="$gray9Light"/>
      }
    </XStack>
  )
}

export default GuessRow
