import React from 'react';
import { Stack, Text } from 'tamagui';

interface TitleBarProps {
    clueWord: string;
}

const TitleBar: React.FC<TitleBarProps> = ({ clueWord }) => {
    return (
        <Stack
            alignItems="center"
            justifyContent="center"
            padding={5}
            backgroundColor="#f0f0f0"
        >
            <Text color='black' fontSize={12} textAlign="center">
                CHANNEL CLUE
            </Text>
            <Text color='black' fontSize={18} textAlign="center">
                “{clueWord}”
            </Text>

        </Stack>
    );
};

export default TitleBar;
