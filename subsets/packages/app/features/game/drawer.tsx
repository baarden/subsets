import { YStack, XStack, Stack, Button, Text, ListItem } from 'tamagui';
import { ChevronRight, XCircle } from '@tamagui/lucide-icons';
import Image from 'next/image';

const Drawer = ({ visible, onClose }) => {
  if (!visible) return null;

  return (
    <Stack themeInverse={true} position="absolute" top={0} left={0} right={0} bottom={0} zIndex={1000}>
      <Stack
        position="absolute"
        top={0}
        left={0}
        right={0}
        bottom={0}
        backgroundColor="rgba(0, 0, 0, 0.5)"
        onPress={onClose}
      />
      <YStack
        position="absolute"
        bottom={0}
        left={0}
        right={0}
        height="90%"
        theme="light"
        backgroundColor='white'
        maxWidth={500}
        marginHorizontal="auto"
        borderTopLeftRadius={16}
        borderTopRightRadius={16}
        padding={16}
      >
        <XStack justifyContent="flex-end">
          <Text fontSize={24} fontWeight="bold" flex={5}>How To Play</Text>
          <Button size="$2" icon={XCircle} theme="light" onPress={onClose} flex={1} />
        </XStack>
        <YStack padding={16}>
          <Text marginTop={8}>Guess the 5 Subsets of the starting word.</Text>
          <Text marginTop={8}>Then use the clue to guess the Subset that crosscuts all 6 words.</Text>
          <YStack marginVertical={16}>
            <ListItem icon={ChevronRight}>
              <Text>Each guess must be a subset of the letters in the previous word.</Text>
            </ListItem>
            <ListItem icon={ChevronRight}>
              <Text>A guess must be a valid word of at least 3 letters.</Text>
            </ListItem>
            <ListItem icon={ChevronRight}>
              <Text>The color of the tile shows how close your guess was to the Subset.</Text>
            </ListItem>
          </YStack>
          <Image src="/example.png" alt="Example" width={266} height={61} />
          <Text><Text fontWeight="bold">S</Text> is not in the word in any spot.</Text>
          <Text><Text fontWeight="bold">E</Text> is in the word and in the correct spot.</Text>
          <Text><Text fontWeight="bold">A</Text> is in the word but in the wrong spot.</Text>
          <Text>The other spaces weren't used, and so don't provide any information.</Text>
        </YStack>
      </YStack>
    </Stack>
  );
};

export default Drawer;
