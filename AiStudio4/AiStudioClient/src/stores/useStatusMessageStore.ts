// AiStudioClient/src/stores/useStatusMessageStore.ts
import { create } from 'zustand';

interface StatusMessageState {
  message: string;
  lastUpdated: number | null;
  setMessage: (message: string) => void;
}

export const useStatusMessageStore = create<StatusMessageState>((set) => ({
  message: '',
  lastUpdated: null,
  setMessage: (message) => set({ 
    message, 
    lastUpdated: message ? Date.now() : null
  }),
}));