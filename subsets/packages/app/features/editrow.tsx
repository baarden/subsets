import { useState, useEffect, useLayoutEffect } from 'react'
import { XStack, AnimatePresence, Text } from 'tamagui'
import Animated, { useSharedValue, withSpring, useAnimatedStyle, Keyframe, runOnJS, useDerivedValue } from 'react-native-reanimated'
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
  
  const animatingShared = useSharedValue<boolean>(true);
  const animatingDerived = useDerivedValue<boolean>(() => { return animatingShared.value; });
  const dragIndexShared = useSharedValue<number | null>(null);
  const dragIndexDerived = useDerivedValue<number | null>(() => { return dragIndexShared.value; })
  const hoverIndexShared = useSharedValue<number | null>(null);
  const hoverIndexDerived = useDerivedValue<number | null>(() => { return hoverIndexShared.value; })
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
//      animatingShared.value = true;
      console.log("animation enabled");  
    }, 2000);
  }, [animatingDerived.value]);

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
        hoverIndexShared.value = span.index;
        console.log(`New index: ${span.index}`);
        return;
      }
    }
    if (hoverIdx !== null) {
      hoverIndexShared.value = null;
    }
  };

  const createPanGesture = (index: number, initialPositionX: number) => {
    return Gesture.Pan()
      .onBegin((event) => {
//        animatingShared.value = true;
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
//        animatingShared.value = false;
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
            dragIndexShared.value = null;
            hoverIndexShared.value = null;
//            animatingShared.value = true;
            console.log("animation enabled");
          }
        );
        translateY.value = withSpring(0, mediumAnimation);
      });
  };

  const handleMoveSquare = (oldIndex: number, newIndex: number) => {
    'worklet';
    const newGuess: Letter[] = [...guess];
    const adjustedNewIndex = oldIndex < newIndex ? newIndex - 1 : newIndex;
    const letterToMove = newGuess[oldIndex];
    newGuess.splice(oldIndex, 1);    
    newGuess.splice(adjustedNewIndex, 0, letterToMove);
    runOnJS(setGuess)(newGuess);
  
    if (onMoveSquare) {
      runOnJS(onMoveSquare)(oldIndex, newIndex);
    }
  };  

  const renderCharacters = () => {
    if (guess.length === 0) { return; }
    return guess.flatMap((letter: Letter, index: number) => {
      const squareKey = keyPrefix + '_square' + index;
      const gapKey = keyPrefix + '_gap' + index;
      const gap = (
        <EmptySquare
          key={gapKey}
          isHoverTarget={index === hoverIndexDerived.value}
          squareDim={squareDim}
          animating={animatingDerived.value}
        />
      );
      const squareProps = {
        letter: letter.char,
        clueType: ClueType.Empty,
        dimension: squareDim,
        squareIndex: index,
        dragIndex: dragIndexDerived.value,
        hoverIndex: hoverIndexDerived.value,
        isAnagramGuess: isAnagramGuess,
        isAnagramLetter: false,
        isHighlighted: false,
        isSwapState: false,
        isAnimating: animatingDerived.value,
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
          <EmptySquare
            isHoverTarget={guess.length === hoverIndexDerived.value}
            squareDim={squareDim}
            animating={animatingDerived.value}
          />
        </XStack>
      </Animated.View>
      <AnimatePresence>
        {dragIndexDerived.value !== null && (
          <Animated.View style={[animatedDragStyle]}>
            <Square
              letter={guess[dragIndexDerived.value].char}
              clueType={ClueType.Empty}
              dimension={squareDim}
              squareIndex={dragIndexDerived.value}
              dragIndex={-1}
              hoverIndex={-1}
              isAnagramGuess={false}
              isAnagramLetter={false}
              isHighlighted={false}
              isEditable={false}
              isSwapState={false}
              isAnimating={animatingDerived.value}
              onPress={() => {}}
            />
          </Animated.View>
      )}
      </AnimatePresence>
    </GestureHandlerRootView>
  )
};

export default EditRow;
