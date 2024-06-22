import React from 'react';
import { Button, Stack, XStack, YStack, Text, Image } from 'tamagui';
import { Info } from '@tamagui/lucide-icons'

interface TitleBarProps {
    clueWord: string;
    onInfoPress: () => void;
}

const TitleBar: React.FC<TitleBarProps> = ({ clueWord, onInfoPress }) => {
    return (
        <XStack justifyContent="space-between" alignItems="center" padding={5} backgroundColor="#f0f0f0">
            <Stack flex={1}>
                <Image src="/logotype.png" alt="Subsets logo" width={66} height={41}/>
            </Stack>
            <YStack
                alignItems="center"
                justifyContent="center"
                padding={5}
                backgroundColor="#f0f0f0"
                flex={10}
            >
                <Text color='black' fontSize={12} textAlign="center">
                    CROSSCUT CLUE
                </Text>
                <Text color='black' fontSize={18} textAlign="center">
                    “{clueWord}”
                </Text>

            </YStack>
            <Button icon={Info} theme="light" onPress={onInfoPress} />
        </XStack>
    );
};

export default TitleBar;
