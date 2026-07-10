"use client"

import { Moon, Sun } from "lucide-react"
import { AnimatePresence, motion } from "motion/react"
import { useSyncExternalStore } from "react"

import { Button } from "@/components/ui/button"
import { useAppStoreContext } from "@/providers/store-provider"

type Theme = 'light' | 'dark'

const themeSequence: Theme[] = ['light', 'dark']

const themeIcons = {
  light: Sun,
  dark: Moon,
}

const getStoredTheme = (): Theme => {
  return localStorage.getItem('theme') === 'dark' ? 'dark' : 'light'
}

const getServerTheme = (): Theme => 'light'

const subscribeToTheme = (callback: () => void) => {
  window.addEventListener('storage', callback)
  window.addEventListener('themechange', callback)

  return () => {
    window.removeEventListener('storage', callback)
    window.removeEventListener('themechange', callback)
  }
}

const applyTheme = (theme: Theme) => {
  document.documentElement.classList.toggle('dark', theme === 'dark')
  localStorage.setItem('theme', theme)
  window.dispatchEvent(new Event('themechange'))
}

export const ThemeSwitcher = () => {
  const setTheme = useAppStoreContext((state) => state.setTheme)
  const theme = useSyncExternalStore(
    subscribeToTheme,
    getStoredTheme,
    getServerTheme,
  )

  const handleToggle = () => {
    const currentIndex = themeSequence.indexOf(theme)
    const nextIndex = (currentIndex + 1) % themeSequence.length
    const nextTheme = themeSequence[nextIndex]
    applyTheme(nextTheme)
    setTheme(nextTheme)
  }

  const Icon = themeIcons[theme]

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={handleToggle}
      aria-label={`Switch theme (current: ${theme})`}
      className="h-9 w-9 rounded-full"
    >
      <AnimatePresence mode="wait" initial={false}>
        <motion.div
          key={theme}
          initial={{ scale: 0.5, rotate: -90, opacity: 0 }}
          animate={{ scale: 1, rotate: 0, opacity: 1 }}
          exit={{ scale: 0.5, rotate: 90, opacity: 0 }}
          transition={{
            duration: 0.2,
            ease: "easeInOut"
          }}
        >
          <Icon className="h-5 w-5" />
        </motion.div>
      </AnimatePresence>
    </Button>
  )
}
