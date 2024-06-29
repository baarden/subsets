import { Stack, Text, createTokens } from 'tamagui'
import { tokens as baseTokens } from '@tamagui/config/v3'
import { ClueType } from '../types/'

interface SquareProps {
  letter?: string
  clueType?: ClueType
  dimension: number
  isAnagramGuess: boolean
  isAnagramLetter: boolean
  isEditable?: boolean
}

const customTokens = createTokens({
  ...baseTokens,
  color: {
    ...baseTokens.color,
    transparent: '#00000000',
  },
})

const Square = ({
  letter = ' ',
  clueType,
  dimension,
  isAnagramGuess,
  isAnagramLetter = false,
  isEditable = false,
  onPress,
}) => {
  const squareMargin = dimension / 15
  const squareHeight = dimension - 2 * squareMargin
  const normalWidth = !isAnagramGuess || isAnagramLetter
  const squareWidth = (normalWidth) ? squareHeight : 0.5 * squareHeight

  const getBackgroundColor = () => {
    switch (clueType) {
      case ClueType.AllCorrect:
        return (normalWidth) ? customTokens.color.blue8Light : customTokens.color.gray8Light
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
      borderColor={isEditable ? 'blue' : "$gray10Light"}
      borderWidth={ 1.5 }
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
