import React from 'react';
import { YStack, XStack, Stack, Button, Image, Text, ScrollView, Anchor, styled } from 'tamagui';
import { ChevronRight, XCircle } from '@tamagui/lucide-icons';
import { GameSettings, ScoringRange } from 'app/types/';

const DefaultText = styled(Text, {
    fontSize: 12,
    textAlign: "left"
  })

interface DrawerProps {
  visible: boolean;
  onClose: () => void;
  config: GameSettings;
}

const Drawer: React.FC<DrawerProps> = ({ visible, onClose, config }) => {
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
          <Text fontSize={18} fontWeight="bold" flex={5}>How to play {config.gameName}</Text>
          <Button size="$2" icon={XCircle} theme="light" onPress={onClose} flex={1} />
        </XStack>
        <ScrollView>
            <YStack padding={16}>
              <XStack width="100%" backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Find all the words in as few guesses as you can.
                </DefaultText>
              </XStack>
              <XStack width="100%" backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  The next word has the same letters — plus one!
                </DefaultText>
              </XStack>
              <DefaultText marginTop={8}>
                If the starting word is "{config.exampleText.startWord}", the answers could look like this:
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src={config.fullExampleImagePath} alt="Full example" width={292} height={218} marginVertical={8}/>
              </YStack>
              <DefaultText>
                The plus-one letter is highlighted in each row.
              </DefaultText>

              <XStack backgroundColor="$blue3Light" marginTop={8}>
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Need a hint? Enter shorter guesses to gather clues!
                </DefaultText>
              </XStack>
              <XStack backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Each guess must be a valid word of 4 letters or more.
                </DefaultText>
              </XStack>
              <XStack backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  After entering your guess, the color of the square
                  shows how close you were to the correct answer.
                </DefaultText>
              </XStack>

              <DefaultText marginTop={8}>
                For the third word, you could guess "{config.exampleText.exampleWord}" to get clues:
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src={config.exampleImagePath} alt="Example" width={205} height={42} marginVertical={8}/>
              </YStack>
              <DefaultText>
                <DefaultText fontWeight="bold">{config.exampleText.correctLetters}</DefaultText>
                : in the word and in the correct square.
              </DefaultText>
              <DefaultText>
                <DefaultText fontWeight="bold">{config.exampleText.wrongLetters}</DefaultText>
                : in the word but in the wrong square.
              </DefaultText>
              <DefaultText>
                <DefaultText fontWeight="bold">{config.exampleText.nonLetters}</DefaultText>
                : not in the word in any square.
              </DefaultText>
              <DefaultText marginTop={8}>The last square isn't used, and doesn't provide a clue.</DefaultText>

              <DefaultText textAlign='center' fontWeight="bold" backgroundColor="$blue4Light" marginTop={16}>
                SOLUTION
              </DefaultText>
              <XStack backgroundColor="$blue3Light" marginTop={8}>
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  When you have all the words, a final plus-one letter is revealed.
                </DefaultText>
              </XStack>
              <XStack backgroundColor="$blue3Light">
                <Stack width={20}><ChevronRight width={15}/></Stack>
                <DefaultText fontWeight="bold" marginTop={4}>
                  Use all the plus-one letters and the provided clue to solve the puzzle!
                </DefaultText>
              </XStack>

              <DefaultText marginTop={8}>
                With the clue below you might guess "{config.exampleText.anagram}":
              </DefaultText>
              <YStack alignItems="center" width="100%">
                <Image src="/anagram.png" alt="Example" width={304} height={358} marginTop={8}/>
              </YStack>

              <DefaultText marginTop={8}>
                A new puzzle is published every day at midnight US Eastern time!
              </DefaultText>

              { config.gameName == "Plus One" &&
                <DefaultText marginTop={8}>
                  If you're looking for something harder, 
                  try <Anchor href={config.siteUrl + "/more"} target="_blank" fontSize={12}>Plus One More</Anchor>:
                  with longer and more challenging words!
                </DefaultText>
              }
              { config.gameName == "Plus One More" &&
                <DefaultText marginTop={8}>
                  If you're looking for something shorter,
                  try the original <Anchor href={config.siteUrl} target="_blank" fontSize={12}>Plus One</Anchor>:
                  with shorter puzzles and everyday words!
                </DefaultText>
              }              

              <DefaultText textAlign='center' fontWeight="bold" backgroundColor="$blue4Light" marginVertical={16}>
                SCORING
              </DefaultText>
              {
                config.scoreRanges.map((range: ScoringRange, index: number) => {
                  const rangeText = (range.max === Infinity) ? `${range.min}+` : `${range.min}-${range.max}`;
                  return (
                    <DefaultText>
                      <DefaultText fontWeight="bold">{rangeText} guesses: </DefaultText>
                      {range.message}
                    </DefaultText>
                  );
                })
              }

            </YStack>
        </ScrollView>
      </YStack>
    </Stack>
  );
};

export default Drawer;
