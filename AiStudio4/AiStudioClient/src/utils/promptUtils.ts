
import { useInputBarStore } from '@/stores/useInputBarStore';

export function setupPromptUtils() {
  window.setPrompt = (text: string) => {
    useInputBarStore.getState().setInputText(text);
    return true;
  };

  window.appendToPrompt = (text: string, options: { newLine?: boolean; replaceCurrent?: boolean } = {}) => {
    const { newLine = true, replaceCurrent = false } = options;

    try {
      if (replaceCurrent) {
        useInputBarStore.getState().setInputText(text);
      } else {
        useInputBarStore.getState().appendToInputText(text, newLine);
      }
      return true;
    } catch (error) {
      console.error('Failed to append to prompt:', error);
      return false;
    }
  };

  window.getPromptText = () => useInputBarStore.getState().inputText;
}

export function cleanupPromptUtils() {
  delete window.setPrompt;
  delete window.appendToPrompt;
  delete window.getPromptText;
}
