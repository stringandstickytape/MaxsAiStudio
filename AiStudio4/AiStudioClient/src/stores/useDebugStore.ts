import { create } from 'zustand';

interface DebugState {
  showAiHiddenContent: boolean;
  toggleAiHiddenContent: () => void;
}

export const useDebugStore = create<DebugState>((set) => ({
  showAiHiddenContent: false,
  toggleAiHiddenContent: () => set((state) => ({ showAiHiddenContent: !state.showAiHiddenContent })),
}));