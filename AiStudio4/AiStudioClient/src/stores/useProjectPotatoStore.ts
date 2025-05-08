// AiStudioClient\src\stores\useProjectPotatoStore.ts
import { create } from 'zustand';

interface ProjectPotatoState {
  isEnabled: boolean;
  setIsEnabled: (enabled: boolean) => void;
}

export const useProjectPotatoStore = create<ProjectPotatoState>((set) => ({
  isEnabled: true, // Default to true
  setIsEnabled: (enabled: boolean) => set({ isEnabled: enabled }),
}));