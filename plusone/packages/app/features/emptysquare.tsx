import { Stack } from 'tamagui';
import Animated, { withSpring, useAnimatedStyle, Keyframe, DerivedValue, SharedValue } from 'react-native-reanimated'

const mediumSpringConfig = {
  damping: 20,
  mass: 0.9,
  stiffness: 100,
  overshootClamping: true,
};

interface EmptySquareProps {
  index: number,
  hoverIndexShared: SharedValue<number | null>,
  squareDim: number,
  animatingShared: SharedValue<boolean>
}

const EmptySquare: React.FC<EmptySquareProps> = ({index, hoverIndexShared, squareDim, animatingShared}) => {
    const squareMargin = squareDim / 15;

    const animatedFlexStyle = useAnimatedStyle(() => {
      const isHoverTarget = (index === hoverIndexShared.value);
      const w = isHoverTarget ? squareDim : 0;
      const o = isHoverTarget ? 1 : 0;
      return isHoverTarget ? {
        width: withSpring(w, mediumSpringConfig),
        opacity: withSpring(o, mediumSpringConfig)
      } : {
        width: w,
        opacity: o
      };
    }, []);
    
    return (
      <Animated.View
        style={[animatedFlexStyle]}
        >
        <Stack
          height={squareDim}
        >
          <Stack
            flexGrow={1}
            margin={squareMargin}
            backgroundColor='$gray5Light'
            borderRadius={5}
            borderWidth={1.5}
            borderColor='$gray7Light'
          />
        </Stack>
      </Animated.View>
    );
  }

export default EmptySquare;
