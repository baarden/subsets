import { YStack, XStack, Stack, Button, Image, Text, ScrollView, styled } from 'tamagui';
import { ChevronRight, XCircle } from '@tamagui/lucide-icons';

const DefaultText = styled(Text, {
    fontSize: 12,
    textAlign: "left"
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
          <Text fontSize={18} fontWeight="bold" flex={5}>How To Play Plus One</Text>
          <Button size="$2" icon={XCircle} theme="light" onPress={onClose} flex={1} />
        </XStack>
        <ScrollView>
            <YStack padding={16}>
              <XStack width="100%" backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Guess the next 5 words by adding 1 letter at each step.
                </DefaultText>
              </XStack>
              <DefaultText marginTop={8}>
                For example, if the starting word is "ACE", the solution might look like this:
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src="/full_example.png" alt="Full example" width={292} height={218} marginVertical={8}/>
              </YStack>
              <DefaultText>
                The "plus one" letter is highlighted in each row.
              </DefaultText>

              <XStack backgroundColor="$blue3Light" marginTop={8}>
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Each guess must be a valid word of 4 letters or more.
                </DefaultText>
              </XStack>
              <XStack backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  The correct guess will fill all the squares.
                  But you can use shorter guesses to gather clues.
                </DefaultText>
              </XStack>
              <XStack backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  The color of the square shows how close your guess was to the correct answer.
                </DefaultText>
              </XStack>

              <DefaultText marginTop={8}>
                For example, if you can't think of a 5-letter word, you could guess "ARCH" to get some clues:
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src="/example.png" alt="Example" width={205} height={42} marginVertical={8}/>
              </YStack>
              <DefaultText><DefaultText fontWeight="bold">A</DefaultText> is in the word and in the correct spot.</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">R, C</DefaultText> are in the word but in the wrong spot.</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">H</DefaultText> is not in the word in any spot.</DefaultText>
              <DefaultText marginTop={8}>The last space isn't used, and doesn't provide a clue.</DefaultText>

              <DefaultText textAlign='center' fontWeight="bold" backgroundColor="$blue4Light" marginTop={16}>SOLUTION</DefaultText>
              <XStack backgroundColor="$blue3Light" marginTop={8}>
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Once you have all 6 words, a final "plus one" letter is revealed.
                  Use all the "plus one" letters and the provided clue to solve the puzzle.
                </DefaultText>
              </XStack>

              <DefaultText marginTop={8}>
                For example, given the clue below you might guess "STROPHE":
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src="/anagram.png" alt="Example" width={255} height={323} marginTop={8}/>
              </YStack>

              <DefaultText textAlign='center' fontWeight="bold" backgroundColor="$blue4Light" marginVertical={16}>SCORING</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">5-10 guesses:</DefaultText> Excellent!</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">11-12 guesses:</DefaultText> Great!</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">13-14 guesses:</DefaultText> Nice!</DefaultText>
              <DefaultText><DefaultText fontWeight="bold">15+ guesses:</DefaultText> Good try!</DefaultText>

            </YStack>
        </ScrollView>
      </YStack>
    </Stack>
  );
};

export default Drawer;
