import { XStack } from 'tamagui'
import { ChevronsUpDown } from '@tamagui/lucide-icons'
import Square from 'app/features/square'
import { Guess, Clue, GuessState, GameSettings } from 'app/types/'

interface GuessRowProps {
  guess: Guess
  isVisible: boolean
  parentWidth: number
  hasHiddenRows: boolean
  squareDim: number
  keyPrefix: string
  config: GameSettings
  onRowPress?: () => void
}

const GuessRow: React.FC<GuessRowProps> = ({
  guess,
  isVisible,
  parentWidth,
  squareDim,
  hasHiddenRows,
  keyPrefix,
  config,
  onRowPress = null
}) => {
  const extraLetterIndex = config.anagramIndex - 1
  const extraLetter: boolean = (guess.wordIndex === extraLetterIndex)
  const rowOpacity = isVisible ? 1 : 0
  const rowHeight = isVisible ? squareDim : 0
  let offset = guess.length
  if (extraLetter) {
    offset = 1
  }
  const leftPad = parentWidth / 2 - offset * squareDim / 2;
  let hLetter : string = guess.highlightLetter;

  const renderCharacters = () => {
    return guess.characters.flatMap((clue: Clue, index: number) => {
      let highlighted = false;
      if (clue.letter === hLetter && guess.state === GuessState.Solved
          && guess.length > 3) {
        highlighted = true;
      }
      const squareKey = keyPrefix + '_square' + index + '_' + guess.key;
      const squareProps = {
        letter: clue.letter,
        clueType: clue.clueType,
        dimension: squareDim,
        squareIndex: index,
        dragIndex: null,
        hoverIndex: null,
        isAnagramGuess: false,
        isAnagramLetter: false,
        isHighlighted: highlighted,
        isEditable: false,
        isSwapState: false,
        onPress: () => {},
      };        
      return (
        <Square key={squareKey} {...squareProps} />
      );
    });
  }  

  return (
    <XStack
      flexDirection="row"
      enterStyle={{height: 0, opacity: 0}}
      height={rowHeight}
      opacity={rowOpacity}
      marginLeft={leftPad}
      backgroundColor="$gray3Light"
      onPress={onRowPress || undefined}
      animation={'medium'}
      >
      {renderCharacters()}
      {guess.state === GuessState.Solved && guess.wordIndex > 0 && hasHiddenRows &&
        <ChevronsUpDown size="$1" marginTop={12} color="$gray9Light"/>
      }
    </XStack>
  )
};

export default GuessRow;
