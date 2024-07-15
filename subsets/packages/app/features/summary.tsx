import { useState } from 'react'
import { Platform } from 'react-native'
import Clipboard from '@react-native-clipboard/clipboard'
import { YStack, XStack, Stack, Button, Image, Text, ScrollView, styled } from 'tamagui';
import { XCircle, Share } from '@tamagui/lucide-icons';
import { Status, Guess, Clue, ClueType, Statistics } from '../types/';

const blueSquare = "\ud83d\udfe6";
const redSquare = "\ud83d\udfe5";
const whiteSquare = "\u2b1c";
const blackSquare = "\u2b1b";
const extraLetterIndex = 6;


const DefaultText = styled(Text, {
    fontSize: 12,
    textAlign: "left"
  })

interface SummaryDrawerProps {
    feedback: string,
    statistics?: Statistics,
    status?: Status,
    visible: boolean,
    onClose: () => void
}

export const SummaryDrawer: React.FC<SummaryDrawerProps> = ({
    feedback,
    statistics,
    status,
    visible,
    onClose
}) => {
    const [shareText, setShareText] = useState<string>("Share")

    const handleSharePress = () => {
        shareStatus()
        setShareText("Copied!")
        setTimeout(() => setShareText("Share"), 3000)
    }

    const shareStatus = () => {
        if (!status) { return }
        let share: string[] = [`Plus One in ${status.guesses.length - 1}!`];
        const guesses = [...status.guesses].sort((a, b) => a.key - b.key)
        guesses.forEach((value: Guess, index: number) => {
            if (value.wordIndex === extraLetterIndex) { return; }
            let row: string = "";
            const indent:number = 3 - Math.ceil(value.wordIndex / 2);
            for (let i:number = 0; i < indent; i++) {
                row += whiteSquare;
            }
            value.characters.map((value: Clue, index: number) => {
                switch (value.clueType) {
                    case ClueType.AllCorrect:
                        row += blueSquare;
                        break;
                    case ClueType.CorrectLetter:
                        row += redSquare;
                        break;
                    case ClueType.Incorrect:
                    case ClueType.Empty:
                        row += blackSquare;
                }
            })
            for (let i:number = indent + value.length; i < 8; i++) {
                row += whiteSquare;
            }
            share.push(row);
        })
        share.push("https://plusone.ngrok.app")
        copyToClipboard(share.join("\n"));
    }

    const copyToClipboard = async (text: string) => {
        if (Platform.OS === 'web') {
            try {
                await navigator.clipboard.writeText(text);
            } catch (err) {
                console.error('Failed to copy: ', err);
            }
        } else {
            Clipboard.setString(text);
        }
    };
          
    if (!visible) return null

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
            <YStack alignItems="center" width="100%">
                <Image src="/logotype.png" alt="Subsets" width={77} height={60} />
                <Button size="$2" icon={XCircle} theme="light" onPress={onClose} position="absolute" right={0} />
            </YStack>
            <ScrollView>
                <YStack padding={16}>

                    <DefaultText fontSize={16} fontWeight={800} marginVertical={16}>Solved! {feedback}</DefaultText>

                    <Stack alignItems='center' width="100%">
                        <Button icon={Share} backgroundColor="$blue4Light" borderWidth={1} width={150} onPress={handleSharePress}>
                            <DefaultText fontWeight={800} >
                                {shareText}
                            </DefaultText>
                        </Button>
                    </Stack>

                    <DefaultText marginVertical={8} fontWeight={800}>STATISTICS</DefaultText>

                    <XStack flex={1}>
                        <YStack flex={1}>
                            <DefaultText>Played</DefaultText>
                            <DefaultText>{statistics?.played}</DefaultText>
                        </YStack>
                        <YStack flex={1}>
                            <DefaultText>Solved</DefaultText>
                            <DefaultText>{statistics?.solved}</DefaultText>
                        </YStack>
                        <YStack flex={1}>
                            <DefaultText>Winning Streak</DefaultText>
                            <DefaultText>{statistics?.streak}</DefaultText>
                        </YStack>
                    </XStack>

                </YStack>
            </ScrollView>
      </YStack>
    </Stack>
  )
}

export default SummaryDrawer
