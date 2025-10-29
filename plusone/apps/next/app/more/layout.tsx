import { Metadata } from 'next'
import { NextTamaguiProvider } from '../NextTamaguiProvider'

export const metadata: Metadata = {
  title: 'Plus One More',
  description: 'The Plus One More word game',
  icons: '/favicon.ico',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <NextTamaguiProvider>{children}</NextTamaguiProvider>
      </body>
    </html>
  )
}
