import { Stack, XStack, Text, Button } from 'tamagui';
import { Share } from '@tamagui/lucide-icons';
import { Status, GameState } from '../types/';

const blueSquare = '\u1F7E6';
const redSquare = '\u1F7E5';
const whiteSquare = '\u2B1C';

const ScoringPanel = (status: Status) => {
  const scoringPanelWidth: number = 140;

    if (!status) {
      return null;
    }
    return (    
      <Stack
        position="absolute"
        top={60}
        right={0}
        width={scoringPanelWidth}
        padding={5}
        height="auto"
        backgroundColor="$green4Light"
        zIndex={0}
      >
        <XStack justifyContent="space-between">
          <Stack flex={1} />
          <Text fontSize={12} flex={1}>GUESSES: {(status?.guesses.length || 1) - 1}</Text>
          { status?.state == GameState.Solved && (
            <Button size="$1" icon={Share} theme="light"/>
          )}
        </XStack>
        { renderGuessRows(10, scoringPanelWidth, hideLetters, showRows, orderByKey) }
      </Stack>
    );
  }

  export default ScoringPanel