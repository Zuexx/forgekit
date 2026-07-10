import type { AppStore } from '../index'
import { ImmerStateCreator } from '../types'

export interface UISlice {
  theme: 'light' | 'dark'
  sidebarOpen: boolean
  loading: boolean
  setTheme: (theme: 'light' | 'dark') => void
  toggleSidebar: () => void
  setSidebarOpen: (open: boolean) => void
  setLoading: (loading: boolean) => void
}

export const createUISlice: ImmerStateCreator<UISlice, AppStore> = (set) => ({
  theme: 'light',
  sidebarOpen: true,
  loading: false,

  setTheme: (theme) =>
    set((state) => {
      state.theme = theme
    }),

  toggleSidebar: () =>
    set((state) => {
      state.sidebarOpen = !state.sidebarOpen
    }),

  setSidebarOpen: (open) =>
    set((state) => {
      state.sidebarOpen = open
    }),

  setLoading: (loading) =>
    set((state) => {
      state.loading = loading
    }),
})
