import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { immer } from 'zustand/middleware/immer'

import { createUISlice, UISlice } from './slices/ui.slice'
import { createUserSlice, UserSlice } from './slices/user.slice'

export type AppStore = UserSlice & UISlice

export const useAppStore = create<AppStore>()(
  devtools(
    immer((...args) => ({
      ...createUserSlice(...args),
      ...createUISlice(...args),
    })),
    { name: 'AppStore' }
  )
)

export * from './slices/ui.slice'
export * from './slices/user.slice'
