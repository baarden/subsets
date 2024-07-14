import React from 'react';
import { Button, Stack, XStack, YStack, Text, Image } from 'tamagui';
import { Info } from '@tamagui/lucide-icons'

interface TitleBarProps {
    guessCount: number;
    onInfoPress: () => void;
}

const TitleBar: React.FC<TitleBarProps> = ({ guessCount, onInfoPress }) => {
    return (
        <XStack
            height={70}
            justifyContent="space-between"
            alignItems="center"
            padding={5}
            backgroundColor="#f0f0f0"
            borderBottomColor="$gray7Light"
            borderBottomWidth={1}
        >
            <Stack position="absolute" left={10}>
                <Image src="/logotype.png" alt="Subsets logo" width={52} height={40} zIndex={5} />
            </Stack>
            <YStack
                alignItems="center"
                justifyContent="center"
                padding={5}
                backgroundColor="#f0f0f0"
                flex={1}
            >
                <Text color='black' fontSize={12} textAlign="center">
                    GUESSES
                </Text>
                <Text color='black' fontSize={18} textAlign="center">
                    {guessCount}
                </Text>

            </YStack>
            <Button icon={Info} theme="light" onPress={onInfoPress} position="absolute" right={0} />
        </XStack>
    );
};

export default TitleBar;
