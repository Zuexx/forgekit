# Store Architecture

This store uses **Zustand with Immer middleware** and follows a **slice pattern**.

## Structure

```
store/
├── types.ts              # TypeScript type definitions
├── index.ts              # Main store export
├── hooks.ts              # Custom hooks for store access
├── slices/
│   ├── user.slice.ts     # User state management
│   └── ui.slice.ts       # UI state management
```

## Usage

### With Provider (Recommended for SSR/Next.js)

```tsx
import { useAppStoreContext } from '@/providers'

function MyComponent() {
  const theme = useAppStoreContext((state) => state.theme)
  const setTheme = useAppStoreContext((state) => state.setTheme)
  
  return <button onClick={() => setTheme('dark')}>Toggle Theme</button>
}
```

### Direct Hook Usage

```tsx
import { useUser, useUI } from '@/store/hooks'

function MyComponent() {
  const { user, setUser, logout } = useUser()
  const { theme, setTheme } = useUI()
  
  return <div>{user?.name}</div>
}
```

## Adding New Slices

1. Create slice file in `store/slices/`:

```typescript
import { ImmerStateCreator } from '../types'

export interface MySlice {
  data: string
  setData: (data: string) => void
}

export const createMySlice: ImmerStateCreator<MySlice> = (set) => ({
  data: '',
  setData: (data) => set((state) => { state.data = data }),
})
```

2. Update `store/index.ts` and `providers/store-provider.tsx` to include the new slice.

## Benefits

- ✅ Immer for immutable updates
- ✅ TypeScript type safety
- ✅ Modular slice pattern
- ✅ DevTools support
- ✅ SSR-safe with provider pattern
