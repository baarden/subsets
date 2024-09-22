import { Stack } from 'tamagui';
import Animated, { useSharedValue, withSpring, useAnimatedStyle, Keyframe } from 'react-native-reanimated'

const mediumSpringConfig = {
  damping: 20,
  mass: 0.9,
  stiffness: 100,
  overshootClamping: true,
};

interface EmptySquareProps {
  isHoverTarget: boolean
  squareDim: number
  animating: boolean
}

const EmptySquare: React.FC<EmptySquareProps> = ({isHoverTarget, squareDim, animating}) => {
    const squareMargin = squareDim / 15;
    const animatingShared = useSharedValue(animating);
    const hoverTargetShared = useSharedValue(isHoverTarget);

    const animatedFlexStyle = useAnimatedStyle(() => {
      const w = hoverTargetShared.value ? squareDim : 0;
      const o = hoverTargetShared.value ? 1 : 0;
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
