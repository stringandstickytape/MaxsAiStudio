// AiStudio4.Web/src/stores/useStatusMessageStore.ts
import { create } from 'zustand';

interface StatusMessageState {
  message: string;
  setMessage: (message: string) => void;
}

export const useStatusMessageStore = create<StatusMessageState>((set) => ({
  message: '',
  setMessage: (message) => set({ message }),
}));