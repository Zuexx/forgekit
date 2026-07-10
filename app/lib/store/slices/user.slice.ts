import type { AppStore } from '../index'
import { ImmerStateCreator } from '../types'

export interface User {
  id: string
  name: string
  email: string
  avatar?: string
}

export interface UserSlice {
  user: User | null
  isAuthenticated: boolean
  setUser: (user: User | null) => void
  updateUser: (updates: Partial<User>) => void
  logout: () => void
}

export const createUserSlice: ImmerStateCreator<UserSlice, AppStore> = (set) => ({
  user: null,
  isAuthenticated: false,

  setUser: (user) =>
    set((state) => {
      state.user = user
      state.isAuthenticated = !!user
    }),

  updateUser: (updates) =>
    set((state) => {
      if (state.user) {
        state.user = { ...state.user, ...updates }
      }
    }),

  logout: () =>
    set((state) => {
      state.user = null
      state.isAuthenticated = false
    }),
})
