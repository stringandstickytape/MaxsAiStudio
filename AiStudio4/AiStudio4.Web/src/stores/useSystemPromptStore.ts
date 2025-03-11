// src/stores/useSystemPromptStore.ts
import { create } from 'zustand';
import { SystemPrompt } from '@/types/systemPrompt';

interface SystemPromptStore {
    prompts: SystemPrompt[];
    defaultPromptId: string | null;
    conversationPrompts: Record<string, string>; // conversationId -> promptId
    currentPrompt: SystemPrompt | null;
    isLibraryOpen: boolean;
    loading: boolean;
    error: string | null;

    setPrompts: (prompts: SystemPrompt[]) => void;
    setCurrentPrompt: (prompt: SystemPrompt | null) => void;
    setDefaultPromptId: (promptId: string) => void;
    setConversationPrompt: (conversationId: string, promptId: string) => void;
    clearConversationPrompt: (conversationId: string) => void;
    toggleLibrary: (isOpen?: boolean) => void;
    setLoading: (loading: boolean) => void;
    setError: (error: string | null) => void;
}

export const useSystemPromptStore = create<SystemPromptStore>((set) => ({
    prompts: [],
    defaultPromptId: null,
    conversationPrompts: {},
    currentPrompt: null,
    isLibraryOpen: false,
    loading: false,
    error: null,

    setPrompts: (prompts) => set((state) => {
        const defaultPrompt = prompts.find(p => p.isDefault);
        return {
            prompts,
            defaultPromptId: state.defaultPromptId || (defaultPrompt ? defaultPrompt.guid : null)
        };
    }),

    setCurrentPrompt: (prompt) => set({ currentPrompt: prompt }),

    setDefaultPromptId: (promptId) => set({ defaultPromptId: promptId }),

    setConversationPrompt: (conversationId, promptId) => set((state) => ({
        conversationPrompts: {
            ...state.conversationPrompts,
            [conversationId]: promptId
        }
    })),

    clearConversationPrompt: (conversationId) => set((state) => {
        const newConversationPrompts = { ...state.conversationPrompts };
        delete newConversationPrompts[conversationId];
        return { conversationPrompts: newConversationPrompts };
    }),

    toggleLibrary: (isOpen) => set((state) => ({
        isLibraryOpen: isOpen !== undefined ? isOpen : !state.isLibraryOpen
    })),

    setLoading: (loading) => set({ loading }),

    setError: (error) => set({ error })
}));

export const debugSystemPromptStore = () => {
    const state = useSystemPromptStore.getState();
    console.group('System Prompt Store Debug');
    console.log('Prompts:', state.prompts);
    console.log('Default Prompt ID:', state.defaultPromptId);
    console.log('Conversation Prompts:', state.conversationPrompts);
    console.log('Current Prompt:', state.currentPrompt);
    console.log('Library Open:', state.isLibraryOpen);
    console.log('Loading:', state.loading);
    console.log('Error:', state.error);
    console.groupEnd();
    return state;
};

(window as any).debugSystemPromptStore = debugSystemPromptStore;