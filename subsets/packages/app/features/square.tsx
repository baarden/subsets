import { Stack, Text, createTokens } from 'tamagui';
import { tokens as baseTokens } from '@tamagui/config/v3';
import Animated, { useSharedValue, withSpring, useAnimatedStyle, Keyframe } from 'react-native-reanimated';
import { Variable } from '@my/ui';
import { ClueType } from 'app/types/';

const customTokens = createTokens({
  ...baseTokens,
  color: {
    ...baseTokens.color,
    transparent: '#00000000',
  },
});

const mediumSpringConfig = {
  damping: 20,
  mass: 0.9,
  stiffness: 100,
  overshootClamping: true,
};

interface SquareProps {
  letter?: string;
  clueType?: ClueType;
  dimension: number;
  squareIndex: number;
  dragIndex: number;
  hoverIndex: number;
  isAnagramGuess: boolean;
  isAnagramLetter: boolean;
  isHighlighted: boolean;
  isEditable?: boolean;
  isSwapState: boolean;
  isAnimating: boolean;
  onPress: () => void;
}

const Square: React.FC<SquareProps> = ({
  letter = ' ',
  clueType,
  dimension,
  squareIndex,
  dragIndex,
  hoverIndex,
  isAnagramGuess,
  isAnagramLetter = false,
  isHighlighted,
  isEditable = false,
  isSwapState,
  isAnimating,
  onPress,
}) => {
  const squareMargin = dimension / 15
  const squareHeight = dimension - 2 * squareMargin
  const normalWidth = !isAnagramGuess || isAnagramLetter
  const squareWidth = (normalWidth) ? squareHeight : 0.5 * squareHeight
  const animatingShared = useSharedValue(isAnimating);
  const dragIndexShared = useSharedValue(dragIndex);
  const hoverIndexShared = useSharedValue(hoverIndex);

  const getBackgroundColor = (): Variable => {
    switch (clueType) {
      case ClueType.AllCorrect:
        return (isHighlighted) ? customTokens.color.blue7Light : customTokens.color.blue5Light;
      case ClueType.CorrectLetter:
        return customTokens.color.orange8Light;
      case ClueType.Incorrect:
        return customTokens.color.gray8Light;
      default:
        return (isSwapState) ? customTokens.color.yellow4Light : customTokens.color.white1;
    }
  };

  const getBorderColor = (): Variable => {
    if (isEditable) {
      return customTokens.color.blue8Dark;
    }
    return customTokens.color.gray9Light;
  }

  const getBorderWidth = () => {
    return (isEditable) ? 3 : 1.5;
  }

  const getHorizontalMargin = () => {
    //if (isEditable) { return 0; }
    if (normalWidth) { return squareMargin; }
    return squareMargin / 2;
  }
  
  const animatedFlexStyle = useAnimatedStyle(() => {
    const w = (dragIndexShared.value === squareIndex && hoverIndexShared.value !== null) ? 0 : dimension;
    const o = (dragIndexShared.value === squareIndex) ? 0 : 1;
    return animatingShared.value ? {
      width: withSpring(w, mediumSpringConfig),
      opacity: withSpring(o, mediumSpringConfig)
    } : {
      width: w,
      opacity: o
    };
  }, []);
  
  return (
    <Animated.View style={[animatedFlexStyle]}>
      <Stack
        width={squareWidth}
        height={squareHeight}
        backgroundColor={getBackgroundColor()}
        borderColor='$gray9Light'
        borderWidth={1.5}
        borderRadius={5}
        marginVertical={squareMargin}
        marginHorizontal={getHorizontalMargin()}
        cursor={'pointer'}
        alignItems="center"
        justifyContent="center"
        display="flex"
        userSelect='none'
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
    </Animated.View>
  );
};

export default Square;
