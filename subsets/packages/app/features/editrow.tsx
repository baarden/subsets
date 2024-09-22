import { useState, useEffect, useLayoutEffect } from 'react'
import { XStack, AnimatePresence, Text } from 'tamagui'
import Animated, { useSharedValue, withSpring, useAnimatedStyle, Keyframe, FadeIn } from 'react-native-reanimated'
import { GestureHandlerRootView, GestureDetector, Gesture } from 'react-native-gesture-handler'
import Square from 'app/features/square'
import EmptySquare from 'app/features/emptysquare'
import { GameSettings } from 'app/types/'
import { ClueType } from 'app/types/'

export interface Letter {
  char: string;
  isPlusOne: boolean;
}

interface EditRowProps {
  clues: string[];
  plusOnes: string[];
  parentWidth: number;
  isAnagramGuess: boolean;
  keyPrefix: string;
  squareDim: number;
  config: GameSettings;
  onMoveSquare?: (oldIndex: number, newIndex: number) => void;
}

const EditRow: React.FC<EditRowProps> = ({
  clues,
  plusOnes,
  isAnagramGuess,
  parentWidth,
  squareDim,
  keyPrefix,
  config,
  onMoveSquare = null
}) => {
  const [guess, setGuess] = useState<Letter[]>([]);
  const squareMargin = squareDim / 15;
  let offset = clues.length + 1
  const leftPad = parentWidth / 2 - offset * squareDim / 2;
  
  const [animating, setAnimating] = useState<boolean>(true);
  const [dragIndexState, setDragIndexState] = useState<number | null>(null);
  const dragIndexShared = useSharedValue<number | null>(null);
  const [hoverIndexState, setHoverIndexState] = useState<number | null>(null);
  const hoverIndexShared = useSharedValue<number | null>(null);
  const translateX = useSharedValue<number>(0);
  const translateY = useSharedValue<number>(0);
  const initialX = useSharedValue<number>(0);
  const mediumAnimation = {
    damping: 20,
    mass: 0.9,
    stiffness: 100,
    overshootClamping: true
  };
  interface hoverSpan {
    min: number
    max: number
    index: number | null
  }
  const hoverSpans = useSharedValue<hoverSpan[]>([]);

  useEffect(() => {
    if (clues.length === 0) { return; }
    hoverSpans.value = calculateGapSpans(null);
    const guessArr: Letter[] = [];
    for (let i = 0; i < clues.length; i++) {
      guessArr.push({char: clues[i], isPlusOne: false});
    }
    guessArr.push({char: "", isPlusOne: true});
    setGuess(guessArr);
  }, [clues]);

  useEffect(() => {
    setTimeout(() => {
      setAnimating(true);
      console.log("animation enabled");  
    }, 2000);
  }, [animating]);

  const calculateGapSpans = (hoverIdx: number | null): hoverSpan[] => {
    let spans: hoverSpan[] = [];
    for (let i = 0; i <= clues.length + 1; i++) {
      const x = leftPad + (i - 0.5) * squareDim;
      const gap: hoverSpan = {min: x, max: x + squareDim, index: i};
      spans.push(gap);
    }
    return spans;
  };

  const checkHover = (x: number) => {
    'worklet';
    const dragIdx = dragIndexShared.value;
    const hoverIdx = hoverIndexShared.value;
    if (dragIdx === null) { return; }
    const maxIdx = hoverSpans.value.length - 1;
    for (let i = 0; i <= maxIdx; i++) {
      const span: hoverSpan = hoverSpans.value[i];
      if ((x >= span.min || i === 0) && (x <= span.max || i === maxIdx)) {
        if (span.index === null) { break; }
        if (span.index === hoverIdx) { return; }
        if (span.index === dragIdx || span.index === dragIdx + 1) { break; }
        setHoverIndexState(span.index);
        hoverIndexShared.value = span.index;
        return;
      }
    }
    if (hoverIdx !== null) {
      setHoverIndexState(null);
      hoverIndexShared.value = null;
    }
  };

  const createPanGesture = (index: number, initialPositionX: number) => {
    return Gesture.Pan()
      .onBegin((event) => {
        setDragIndexState(index);
        dragIndexShared.value = index;
        initialX.value = event.absoluteX - initialPositionX;
        translateX.value = initialPositionX + squareMargin;
      })
      .onUpdate((event) => {
        checkHover(event.absoluteX);
        translateX.value = event.absoluteX - initialX.value;
        translateY.value = 10;
      })
      .onEnd(() => {
        setAnimating(false);
        console.log("animation disabled");
        let hoverIdx = hoverIndexShared.value;
        if (dragIndexShared.value !== null && hoverIdx !== null) {
          if (hoverIdx > dragIndexShared.value) {
            hoverIdx--;
          }
          handleMoveSquare(dragIndexShared.value, hoverIdx);
        }
        const endPosition = (hoverIdx === null) ? initialPositionX : leftPad + hoverIdx * squareDim - squareMargin;
        translateX.value = withSpring(
          endPosition + squareMargin,
          mediumAnimation,
          () => {
            setDragIndexState(null);
            dragIndexShared.value = null;
            setHoverIndexState(null);
            hoverIndexShared.value = null;
          }
        );
        translateY.value = withSpring(0, mediumAnimation);
      });
  };

  const handleMoveSquare = (oldIndex: number, newIndex: number) => {
    let letters = structuredClone(guess);
    const insIndex = (oldIndex > newIndex) ? newIndex : newIndex + 1;
    const delIndex = (newIndex > oldIndex) ? oldIndex : oldIndex + 1;
    letters.splice(insIndex, 0, guess[oldIndex]);
    letters.splice(delIndex, 1);
    let newGuess = structuredClone(guess);
    for (let i=0; i < guess.length; i++) {
      newGuess[i].char = letters[i].char;
      newGuess[i].isPlusOne = letters[i].isPlusOne;
    }
    console.log(guess);
    console.log(newGuess);
    setGuess(newGuess);

    if (onMoveSquare === null) { return; }
    onMoveSquare(oldIndex, newIndex);
  }

  const renderCharacters = () => {
    if (guess.length === 0) { return; }
    return guess.flatMap((letter: Letter, index: number) => {
      const squareKey = keyPrefix + '_square' + index;
      const gapKey = keyPrefix + '_gap' + index;
      const gap = (
        <EmptySquare key={gapKey} isHoverTarget={index === hoverIndexState} squareDim={squareDim} animating={animating} />
      );
      const squareProps = {
        letter: letter.char,
        clueType: ClueType.Empty,
        dimension: squareDim,
        squareIndex: index,
        dragIndex: dragIndexState,
        hoverIndex: hoverIndexState,
        isAnagramGuess: isAnagramGuess,
        isAnagramLetter: false,
        isHighlighted: false,
        isSwapState: false,
        isAnimating: animating,
        onPress: () => {},
      };
      const square = (
        <GestureDetector
          key={squareKey}
          gesture={createPanGesture(index, leftPad + index * squareDim - squareMargin)}
        >
          <Square {...squareProps} />
        </GestureDetector>
      );
      return [gap, square];
    });
  }  

  const enterKeyframe = new Keyframe({
    0: { height: 0 },
    100: { height: squareDim }
  });

  const testKeyframe = new Keyframe({
    0: { opacity: 0 },
    100: { opacity: 1 }
  });

  const exitKeyframe = new Keyframe({
    0: { height: squareDim },
    100: { height: 0 }
  });

  const animatedDragStyle = useAnimatedStyle(() => {
    return {
      transform: [
        { translateX: translateX.value },
        { translateY: translateY.value }
      ],
      position: 'absolute',
      zIndex: 10,
    };
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1}}>
      <Animated.View entering={testKeyframe} >
        <XStack
          flexDirection="row"
          marginLeft={leftPad}
          backgroundColor="$gray3Light"
        >
          {renderCharacters()}
          <EmptySquare isHoverTarget={guess.length === hoverIndexState} squareDim={squareDim} animating={animating} />
        </XStack>
      </Animated.View>
      <AnimatePresence>
        {dragIndexState !== null && (
          <Animated.View style={[animatedDragStyle]}>
            <Square
              letter={guess[dragIndexState].char}
              clueType={ClueType.Empty}
              dimension={squareDim}
              squareIndex={dragIndexState}
              dragIndex={-1}
              hoverIndex={-1}
              isAnagramGuess={false}
              isAnagramLetter={false}
              isHighlighted={false}
              isEditable={false}
              isSwapState={false}
              isAnimating={animating}
              onPress={() => {}}
            />
          </Animated.View>
      )}
      </AnimatePresence>
    </GestureHandlerRootView>
  )
};

export default EditRow;
