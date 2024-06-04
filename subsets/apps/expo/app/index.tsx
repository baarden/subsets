import { HomeScreen } from 'app/features/home/screen'
import { GameComponent } from 'app/features/game/game'
import { Stack } from 'expo-router'

export default function Screen() {
  return (
    <>
      <Stack.Screen
        options={{
          title: 'Word Delta',
        }}
      />
      <GameComponent />
    </>
  )
}
