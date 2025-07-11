/* eslint-disable no-unused-vars */
// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\AiStudioClient\src\stores\useVoiceInputStore.ts
import { create } from "zustand";

interface VoiceInputState {
  isListening: boolean;
  error: string | null;
  baseText: string; // Text that existed before voice input started
  startListening: () => void;
  stopListening: () => void;
  setError: (error: string | null) => void;
  setBaseText: (text: string) => void;
  _setIsListening: (status: boolean) => void; // Internal action for hook updates
}

export const useVoiceInputStore = create<VoiceInputState>((set, get) => ({
  isListening: false,
  error: null,
  baseText: '',
  startListening: () => {
    // This action primarily signals intent. The actual mic capture
    // will be triggered by an effect in InputBar.tsx watching isListening.
    // It also needs to ensure that if it's already listening, calling it again
    // doesn't cause issues, though the primary toggle logic will be in InputBar's handleToggleListening.
    // For now, it just sets the state.
    set({ isListening: true, error: null });
  },
  stopListening: () => {
    // Similar to startListening, this signals intent.
    // The actual mic stop will be triggered by an effect or direct call in InputBar.tsx.
    set({ isListening: false, baseText: '' }); // Clear baseText when stopping
  },
  setError: (error: string | null) => {
    set({ error });
  },
  setBaseText: (text: string) => {
    set({ baseText: text });
  },
  _setIsListening: (status: boolean) => {
    // This is intended to be called by the useVoiceInput hook
    // to reflect the actual state of the SpeechRecognition API.
    set({ isListening: status });
  },
}));