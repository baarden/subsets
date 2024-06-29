import { YStack, XStack, Stack, Button, Image, Text, ListItem, ScrollView, styled } from 'tamagui';
import { ChevronRight, XCircle } from '@tamagui/lucide-icons';

const DefaultText = styled(Text, {
    fontSize: 12,
    textAlign: "left"
  })

const DefaultListItem = styled(ListItem, {
    backgroundColor: 'unset',
    marginTop: 5,
})

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
          <Text fontSize={18} fontWeight="bold" flex={5}>How To Play Subsets</Text>
          <Button size="$2" icon={XCircle} theme="light" onPress={onClose} flex={1} />
        </XStack>
        <ScrollView>
            <YStack padding={16}>
                <DefaultText fontStyle="italic" backgroundColor="$blue4Light" padding={4}>
                    <DefaultText fontWeight="bold">Subset:</DefaultText> a word using only letters from another longer word.
                </DefaultText>

                <DefaultText marginTop={8}>Guess the 5 Subsets of the starting word.</DefaultText>
                <YStack marginTop={8}>
                    <DefaultListItem icon={ChevronRight} size="$1">
                        <DefaultText flex={1}>Each guess must be a subset of the previous word.</DefaultText>
                    </DefaultListItem>
                    <DefaultListItem icon={ChevronRight} size="$1">
                        <DefaultText flex={1}>
                            A guess must be a valid word of at least 3 letters.
                        </DefaultText>
                    </DefaultListItem>
                    <DefaultListItem icon={ChevronRight} size="$1">
                        <DefaultText flex={1}>
                            The correct guess will fill all the spaces.
                            But you can use shorter guesses to gather clues.
                        </DefaultText>
                    </DefaultListItem>
                    <DefaultListItem icon={ChevronRight} size="$1">
                        <DefaultText flex={1}>The color of the tile shows how close your guess was to the correct answer.</DefaultText>
                    </DefaultListItem>
                </YStack>

                <YStack alignItems="center" width="100%">
                  <Image src="/example.png" alt="Example" width={177} height={40} marginTop={16}/>
                </YStack>
                <DefaultText><DefaultText fontWeight="bold">S</DefaultText> is not in the word in any spot.</DefaultText>
                <DefaultText><DefaultText fontWeight="bold">E</DefaultText> is in the word and in the correct spot.</DefaultText>
                <DefaultText><DefaultText fontWeight="bold">A</DefaultText> is in the word but in the wrong spot.</DefaultText>
                <DefaultText marginTop={8}>The other spaces weren't used, and so don't provide any information.</DefaultText>

                <DefaultText textAlign='center' fontWeight="bold" backgroundColor="$blue4Light" marginTop={16}>SOLUTION</DefaultText>
                <DefaultText marginTop={8}>Once you have all 6 words, solve the puzzle by finding the Subset in the
                    highlighted letters that matches the Crosscut Clue at the top of the screen.
                    In the example below, you might guess "INSIDE":</DefaultText>
                <YStack alignItems="center" width="100%">
                  <Image src="/anagram.png" alt="Example" width={249} height={217} marginTop={8}/>
                </YStack>
            </YStack>
        </ScrollView>
      </YStack>
    </Stack>
  );
};

export default Drawer;
