import { create } from 'zustand';

interface DebugState {
  showDevContentView: boolean;
  toggleDevContentView: () => void;
}

export const useDebugStore = create<DebugState>((set) => ({
  showDevContentView: false,
  toggleDevContentView: () => set((state) => ({ showDevContentView: !state.showDevContentView })),
}));