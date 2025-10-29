import { GameComponent } from 'app/features/'
import { Stack } from 'expo-router'

export default function Screen() {
  return (
    <>
      <Stack.Screen
        options={{
          title: 'Plus One',
        }}
      />
      <GameComponent path=""/>
    </>
  )
}
