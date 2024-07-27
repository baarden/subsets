import { Stack, Text, createTokens } from 'tamagui'
import { tokens as baseTokens } from '@tamagui/config/v3'
import { Variable } from '@my/ui'
import { ClueType } from 'app/types/'

const customTokens = createTokens({
  ...baseTokens,
  color: {
    ...baseTokens.color,
    transparent: '#00000000',
  },
})

interface SquareProps {
  letter?: string
  clueType?: ClueType
  dimension: number
  isAnagramGuess: boolean
  isAnagramLetter: boolean
  isHighlighted: boolean
  isEditable?: boolean,
  isSwapState: boolean,
  onPress: () => void
}

const Square: React.FC<SquareProps> = ({
  letter = ' ',
  clueType,
  dimension,
  isAnagramGuess,
  isAnagramLetter = false,
  isHighlighted,
  isEditable = false,
  isSwapState,
  onPress,
}) => {
  const squareMargin = dimension / 15
  const squareHeight = dimension - 2 * squareMargin
  const normalWidth = !isAnagramGuess || isAnagramLetter
  const squareWidth = (normalWidth) ? squareHeight : 0.5 * squareHeight

  const getBackgroundColor = (): Variable => {
    switch (clueType) {
      case ClueType.AllCorrect:
        return (isHighlighted) ? customTokens.color.blue7Light : customTokens.color.blue5Light
      case ClueType.CorrectLetter:
        return customTokens.color.orange8Light
      case ClueType.Incorrect:
        return customTokens.color.gray8Light
      default:
        return customTokens.color.white1
    }
  }

  const getBorderColor = (): Variable => {
    if (isEditable) {
      return customTokens.color.blue8Dark;
    }
    return customTokens.color.gray9Light;
  }

  const getBorderWidth = () => {
    return (isEditable) ? 3 : 1.5;
  }

  return (
    <Stack>

    <Stack
      width={squareWidth}
      height={squareHeight}
      backgroundColor={getBackgroundColor()}
      borderColor={getBorderColor()}
      borderWidth={getBorderWidth()}
      borderRadius={5}
      marginVertical={squareMargin}
      marginHorizontal={ normalWidth ? squareMargin : squareMargin / 2 }
      cursor={'pointer'}
      alignItems="center"
      justifyContent="center"
      display="flex"
      onPress={onPress}
      zIndex={2}
    >
      <Text position='absolute'
        zIndex={5}
        color={'black'}
        fontSize={18}
        fontWeight={normalWidth ? "bold" : "unset"}
      >
        {letter.toUpperCase()}
      </Text>
    </Stack>

    { isSwapState && ! isEditable &&
      <Stack
        position='absolute'
        top={1}
        left={1}
        backgroundColor="$blue7Light"
        borderRadius={6}
        height={squareHeight + 4}
        width={squareWidth + 4}
        zIndex={1}
      />
    }

    </Stack>
  )
}

export default Square
