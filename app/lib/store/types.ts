import { StateCreator } from 'zustand'

export type ImmerStateCreator<T, U = T> = StateCreator<
  U,
  [['zustand/immer', never], never],
  [],
  T
>
