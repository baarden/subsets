import { Stack, Text, createTokens } from 'tamagui'
import { tokens as baseTokens } from '@tamagui/config/v3'
import LottieView from 'lottie-react-native';
import { Variable } from '@my/ui'
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

  const getBackgroundColor = (): Variable => {
    switch (clueType) {
      case ClueType.AllCorrect:
        return (normalWidth) ? customTokens.color.blue7Light : customTokens.color.blue5Light
      case ClueType.CorrectLetter:
        return customTokens.color.orange8Light
      case ClueType.Incorrect:
        return customTokens.color.gray8Light
      default:
        return customTokens.color.transparent
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
    >
      { isHighlighted &&
        <LottieView
          source={require("../assets/shimmer.json")}
          autoPlay
          width={squareWidth}
          height={squareHeight}
          loop
          speed={1.0 + 0.5 * (Math.random() - 0.5)}
          position='absolute'
          top={0}
          left={0}
        />
      }
      <Text position='absolute' zIndex={5} color={'black'} fontSize={18} fontWeight={normalWidth ? "bold" : "unset"}>
        {letter.toUpperCase()}</Text>
    </Stack>
  )
}

export default Square
