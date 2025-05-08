import { create } from 'zustand';
import { SystemPrompt } from '@/types/systemPrompt';

interface SystemPromptStore {
  prompts: SystemPrompt[];
  defaultPromptId: string | null;
  convPrompts: Record<string, string>; 
  currentPrompt: SystemPrompt | null;
  isLibraryOpen: boolean;
  loading: boolean;
  error: string | null;

  setPrompts: (prompts: SystemPrompt[]) => void;
  setCurrentPrompt: (prompt: SystemPrompt | null) => void;
  setDefaultPromptId: (promptId: string) => void;
  setConvPrompt: (convId: string, promptId: string) => void;
  clearConvPrompt: (convId: string) => void;
  toggleLibrary: (isOpen?: boolean) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useSystemPromptStore = create<SystemPromptStore>((set) => ({
  prompts: [],
  defaultPromptId: null,
  convPrompts: {},
  currentPrompt: null,
  isLibraryOpen: false,
  loading: false,
  error: null,

  setPrompts: (prompts) =>
    set((state) => {
      const defaultPrompt = prompts.find((p) => p.isDefault);
        
      
      if (prompts.length > 0) {
        window.dispatchEvent(new CustomEvent('system-prompts-updated'));
      }
        
      return {
        prompts,
        defaultPromptId: state.defaultPromptId || (defaultPrompt ? defaultPrompt.guid : null),
      };
    }),

  setCurrentPrompt: (prompt) => set({ currentPrompt: prompt }),

  setDefaultPromptId: (promptId) => set({ defaultPromptId: promptId }),

  setConvPrompt: (convId, promptId) =>
    set((state) => ({
      convPrompts: {
        ...state.convPrompts,
        [convId]: promptId,
      },
    })),

  clearConvPrompt: (convId) =>
    set((state) => {
      const newConvPrompts = { ...state.convPrompts };
      delete newConvPrompts[convId];
      return { convPrompts: newConvPrompts };
    }),

  toggleLibrary: (isOpen) =>
    set((state) => ({
      isLibraryOpen: isOpen !== undefined ? isOpen : !state.isLibraryOpen,
    })),

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),
}));

export const debugSystemPromptStore = () => {
  const state = useSystemPromptStore.getState();
  console.group('System Prompt Store Debug');
  console.log('Prompts:', state.prompts);
  console.log('Default Prompt ID:', state.defaultPromptId);
  console.log('Conv Prompts:', state.convPrompts);
  console.log('Current Prompt:', state.currentPrompt);
  console.log('Library Open:', state.isLibraryOpen);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

(window as any).debugSystemPromptStore = debugSystemPromptStore;