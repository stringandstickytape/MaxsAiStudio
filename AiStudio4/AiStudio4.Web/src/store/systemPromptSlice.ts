// src/store/systemPromptSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { SystemPrompt, SystemPromptState } from '@/types/systemPrompt';

const initialState: SystemPromptState = {
    prompts: [],
    defaultPromptId: null,
    conversationPrompts: {},
    loading: false,
    error: null,
    currentPrompt: null,
    isLibraryOpen: false
};

const systemPromptSlice = createSlice({
    name: 'systemPrompts',
    initialState,
    reducers: {
        setCurrentPrompt: (state, action: PayloadAction<SystemPrompt | null>) => {
            state.currentPrompt = action.payload;
        },
        toggleLibrary: (state, action: PayloadAction<boolean | undefined>) => {
            state.isLibraryOpen = action.payload !== undefined ? action.payload : !state.isLibraryOpen;
        },
        clearError: (state) => {
            state.error = null;
        },
        // Add these actions to sync with the RTK Query state if needed
        setPrompts: (state, action: PayloadAction<SystemPrompt[]>) => {
            state.prompts = action.payload;
            state.defaultPromptId = action.payload.find(p => p.isDefault)?.guid || null;
        },
        setDefaultPromptId: (state, action: PayloadAction<string>) => {
            state.defaultPromptId = action.payload;
        },
        setConversationPrompt: (state, action: PayloadAction<{ conversationId: string; promptId: string }>) => {
            state.conversationPrompts[action.payload.conversationId] = action.payload.promptId;
        },
        clearConversationPrompt: (state, action: PayloadAction<string>) => {
            delete state.conversationPrompts[action.payload];
        },
    },
});

export const {
    setCurrentPrompt,
    toggleLibrary,
    clearError,
    setPrompts,
    setDefaultPromptId,
    setConversationPrompt,
    clearConversationPrompt
} = systemPromptSlice.actions;

export default systemPromptSlice.reducer;