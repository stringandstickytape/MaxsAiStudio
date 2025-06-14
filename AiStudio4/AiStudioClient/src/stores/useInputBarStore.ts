import { create } from 'zustand';
import { subscribeWithSelector } from 'zustand/middleware';

interface InputBarState {
  inputText: string;
  requestFocus: boolean;
  setInputText: (text: string) => void;
  appendToInputText: (text: string, newLine?: boolean) => void;
  clearInputText: () => void;
  setRequestFocus: (request: boolean) => void;
}

export const useInputBarStore = create<InputBarState>()(
  subscribeWithSelector((set, get) => ({
    inputText: '',
    requestFocus: false,
    setInputText: (text) => set({ inputText: text }),
    appendToInputText: (text, newLine = true) => {
      set((state) => ({
        inputText: state.inputText + (newLine && state.inputText ? '\n' : '') + text,
        requestFocus: true,
      }));
    },
    clearInputText: () => set({ inputText: '' }),
    setRequestFocus: (request) => set({ requestFocus: request }),
  }))
);

export const useInputText = () => useInputBarStore((state) => state.inputText);
export const useInputActions = () => useInputBarStore((state) => ({
  setInputText: state.setInputText,
  appendToInputText: state.appendToInputText,
  clearInputText: state.clearInputText,
}));