import { Stack, Text, createTokens } from 'tamagui'
import { tokens as baseTokens } from '@tamagui/config/v3'
import { ClueType } from '../types/'

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
  onPress,
}) => {
  const squareMargin = dimension / 15
  const squareHeight = dimension - 2 * squareMargin
  const normalWidth = !isAnagramGuess || isAnagramLetter
  const squareWidth = (normalWidth) ? squareHeight : 0.5 * squareHeight

  const getBackgroundColor = () => {
    if (isHighlighted) {
      return customTokens.color.yellow6Light
    }
    switch (clueType) {
      case ClueType.AllCorrect:
        return customTokens.color.blue7Light //: customTokens.color.gray8Light
      case ClueType.CorrectLetter:
        return customTokens.color.orange8Light
      case ClueType.Incorrect:
        return customTokens.color.gray8Light
      default:
        return customTokens.color.transparent
    }
  }

  return (
    <Stack
      width={squareWidth}
      height={squareHeight}
      backgroundColor={getBackgroundColor()}
      borderColor={isEditable ? 'blue' : "$gray8Light"}
      borderWidth={ isEditable ? 3 : 1.5 }
      borderRadius={5}
      marginVertical={squareMargin}
      marginHorizontal={ normalWidth ? squareMargin : squareMargin / 2 }
      cursor={'pointer'}
      alignItems="center"
      justifyContent="center"
      display="flex"
      onPress={onPress}
    >
      <Text color={'black'} fontSize={18} fontWeight={normalWidth ? "bold" : "unset"}>
        {letter.toUpperCase()}</Text>
    </Stack>
  )
}

export default Square
