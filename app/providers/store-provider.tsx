"use client"

import { createContext, ReactNode, useContext, useState } from 'react'
import { useStore } from 'zustand'
import { devtools } from 'zustand/middleware'
import { immer } from 'zustand/middleware/immer'
import { createStore } from 'zustand/vanilla'

import { createUISlice, createUserSlice, UISlice, UserSlice } from '@/lib/store'

export type AppStore = UserSlice & UISlice

export type AppStoreApi = ReturnType<typeof createAppStore>

export const createAppStore = () => {
  return createStore<AppStore>()(
    devtools(
      immer((...args) => ({
        ...createUserSlice(...args),
        ...createUISlice(...args),
      })),
      { name: 'AppStore' }
    )
  )
}

export const AppStoreContext = createContext<AppStoreApi | undefined>(undefined)

export interface AppStoreProviderProps {
  children: ReactNode
}

export function AppStoreProvider({ children }: AppStoreProviderProps) {
  const [store] = useState(createAppStore)

  return (
    <AppStoreContext.Provider value={store}>
      {children}
    </AppStoreContext.Provider>
  )
}

export const useAppStoreContext = <T,>(
  selector: (store: AppStore) => T,
): T => {
  const appStoreContext = useContext(AppStoreContext)

  if (!appStoreContext) {
    throw new Error('useAppStoreContext must be used within AppStoreProvider')
  }

  return useStore(appStoreContext, selector)
}
