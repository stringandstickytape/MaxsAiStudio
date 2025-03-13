// src/stores/useUserPromptStore.ts
import { create } from 'zustand';
import { UserPrompt } from '@/types/userPrompt';

interface UserPromptStore {
  prompts: UserPrompt[];
  favoritePromptIds: string[];
  currentPrompt: UserPrompt | null;
  isLibraryOpen: boolean;
  loading: boolean;
  error: string | null;

  setPrompts: (prompts: UserPrompt[]) => void;
  setCurrentPrompt: (prompt: UserPrompt | null) => void;
  toggleFavorite: (promptId: string) => void;
  toggleLibrary: (isOpen?: boolean) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useUserPromptStore = create<UserPromptStore>((set) => ({
  prompts: [],
  favoritePromptIds: [],
  currentPrompt: null,
  isLibraryOpen: false,
  loading: false,
  error: null,

  setPrompts: (prompts) =>
    set((state) => {
      const favoritePromptIds = prompts
        .filter(p => p.isFavorite)
        .map(p => p.guid);
      return { prompts, favoritePromptIds };
    }),

  setCurrentPrompt: (prompt) => set({ currentPrompt: prompt }),

  toggleFavorite: (promptId) =>
    set((state) => {
      const updatedPrompts = state.prompts.map(prompt => 
        prompt.guid === promptId 
          ? { ...prompt, isFavorite: !prompt.isFavorite }
          : prompt
      );
      
      const favoritePromptIds = updatedPrompts
        .filter(p => p.isFavorite)
        .map(p => p.guid);

      return { prompts: updatedPrompts, favoritePromptIds };
    }),

  toggleLibrary: (isOpen) =>
    set((state) => ({
      isLibraryOpen: isOpen !== undefined ? isOpen : !state.isLibraryOpen,
    })),

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),
}));

export const debugUserPromptStore = () => {
  const state = useUserPromptStore.getState();
  console.group('User Prompt Store Debug');
  console.log('Prompts:', state.prompts);
  console.log('Favorite Prompt IDs:', state.favoritePromptIds);
  console.log('Current Prompt:', state.currentPrompt);
  console.log('Library Open:', state.isLibraryOpen);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

(window as any).debugUserPromptStore = debugUserPromptStore;
