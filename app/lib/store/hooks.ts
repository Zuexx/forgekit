import { useAppStore } from './index'

export const useUser = () => useAppStore((state) => ({
  user: state.user,
  isAuthenticated: state.isAuthenticated,
  setUser: state.setUser,
  updateUser: state.updateUser,
  logout: state.logout,
}))

export const useUI = () => useAppStore((state) => ({
  theme: state.theme,
  sidebarOpen: state.sidebarOpen,
  loading: state.loading,
  setTheme: state.setTheme,
  toggleSidebar: state.toggleSidebar,
  setSidebarOpen: state.setSidebarOpen,
  setLoading: state.setLoading,
}))

export const useTheme = () => useAppStore((state) => state.theme)
export const useSidebarOpen = () => useAppStore((state) => state.sidebarOpen)
export const useLoading = () => useAppStore((state) => state.loading)
export const useIsAuthenticated = () => useAppStore((state) => state.isAuthenticated)
