// AiStudio4.Web\src\stores\useJumpToEndStore.ts
import { create } from 'zustand';

interface JumpToEndState {
  jumpToEndEnabled: boolean;
  setJumpToEndEnabled: (enabled: boolean) => void;
}

export const useJumpToEndStore = create<JumpToEndState>((set) => ({
  jumpToEndEnabled: true, // Default to true (auto-scroll enabled)
  setJumpToEndEnabled: (enabled: boolean) => set({ jumpToEndEnabled: enabled }),
}));