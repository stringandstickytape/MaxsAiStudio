// src/store/systemPromptSlice.ts
import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { SystemPrompt, SystemPromptState } from '@/types/systemPrompt';
import { SystemPromptService } from '@/services/SystemPromptService';

// Helper function to normalize prompt properties
const normalizePrompt = (prompt: any): SystemPrompt => {
    if (!prompt) return null;

    return {
        guid: prompt.guid || prompt.Guid,
        title: prompt.title || prompt.Title,
        content: prompt.content || prompt.Content,
        description: prompt.description || prompt.Description,
        isDefault: prompt.isDefault || prompt.IsDefault,
        createdDate: prompt.createdDate || prompt.CreatedDate,
        modifiedDate: prompt.modifiedDate || prompt.ModifiedDate,
        tags: prompt.tags || prompt.Tags || [],
    };
};

// Async thunks
export const fetchSystemPrompts = createAsyncThunk(
    'systemPrompts/fetchAll',
    async () => {
        const prompts = await SystemPromptService.getSystemPrompts();
        return prompts.map(normalizePrompt);
    }
);

export const fetchSystemPromptById = createAsyncThunk(
    'systemPrompts/fetchById',
    async (promptId: string) => {
        const prompt = await SystemPromptService.getSystemPromptById(promptId);
        return normalizePrompt(prompt);
    }
);

export const createSystemPrompt = createAsyncThunk(
    'systemPrompts/create',
    async (promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>) => {
        try {
            const newPrompt = await SystemPromptService.createSystemPrompt(promptData);
            console.log("Created prompt from server:", newPrompt);
            return normalizePrompt(newPrompt);
        } catch (error) {
            console.error("Error in createSystemPrompt thunk:", error);
            throw error;
        }
    }
);

export const updateSystemPrompt = createAsyncThunk(
    'systemPrompts/update',
    async (promptData: SystemPrompt) => {
        const updatedPrompt = await SystemPromptService.updateSystemPrompt(promptData);
        return normalizePrompt(updatedPrompt);
    }
);

export const deleteSystemPrompt = createAsyncThunk(
    'systemPrompts/delete',
    async (promptId: string) => {
        await SystemPromptService.deleteSystemPrompt(promptId);
        return promptId;
    }
);

export const setDefaultSystemPrompt = createAsyncThunk(
    'systemPrompts/setDefault',
    async (promptId: string) => {
        await SystemPromptService.setDefaultSystemPrompt(promptId);
        return promptId;
    }
);

export const getConversationSystemPrompt = createAsyncThunk(
    'systemPrompts/getConversationPrompt',
    async (conversationId: string) => {
        const prompt = await SystemPromptService.getConversationSystemPrompt(conversationId);
        return { conversationId, prompt: normalizePrompt(prompt) };
    }
);

export const setConversationSystemPrompt = createAsyncThunk(
    'systemPrompts/setConversationPrompt',
    async ({ conversationId, promptId }: { conversationId: string; promptId: string }) => {
        console.log(`setConversationSystemPrompt thunk called with conversationId=${conversationId}, promptId=${promptId}`);
        if (!promptId) {
            throw new Error('Prompt ID is required');
        }
        await SystemPromptService.setConversationSystemPrompt(conversationId, promptId);
        return { conversationId, promptId };
    }
);

export const clearConversationSystemPrompt = createAsyncThunk(
    'systemPrompts/clearConversationPrompt',
    async (conversationId: string) => {
        await SystemPromptService.clearConversationSystemPrompt(conversationId);
        return conversationId;
    }
);

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
            state.currentPrompt = action.payload ? normalizePrompt(action.payload) : null;
        },
        toggleLibrary: (state, action: PayloadAction<boolean | undefined>) => {
            state.isLibraryOpen = action.payload !== undefined ? action.payload : !state.isLibraryOpen;
        },
        clearError: (state) => {
            state.error = null;
        }
    },
    extraReducers: (builder) => {
        // Fetch all system prompts
        builder
            .addCase(fetchSystemPrompts.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchSystemPrompts.fulfilled, (state, action) => {
                state.loading = false;
                state.prompts = action.payload;
                state.defaultPromptId = action.payload.find(p => p.isDefault)?.guid || null;
            })
            .addCase(fetchSystemPrompts.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to fetch system prompts';
            })

            // Fetch system prompt by ID
            .addCase(fetchSystemPromptById.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchSystemPromptById.fulfilled, (state, action) => {
                state.loading = false;
                state.currentPrompt = action.payload;
                // Also update in the prompts array if it exists
                const index = state.prompts.findIndex(p => p.guid === action.payload.guid);
                if (index !== -1) {
                    state.prompts[index] = action.payload;
                } else {
                    state.prompts.push(action.payload);
                }
            })
            .addCase(fetchSystemPromptById.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to fetch system prompt';
            })

            // Create system prompt
            .addCase(createSystemPrompt.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(createSystemPrompt.fulfilled, (state, action) => {
                state.loading = false;
                const newPrompt = normalizePrompt(action.payload);
                console.log("Normalized new prompt:", newPrompt);

                if (newPrompt && newPrompt.guid) {
                    state.prompts.push(newPrompt);
                    state.currentPrompt = newPrompt;

                    if (newPrompt.isDefault) {
                        state.defaultPromptId = newPrompt.guid;
                        // Update other prompts to not be default
                        state.prompts.forEach(prompt => {
                            if (prompt.guid !== newPrompt.guid) {
                                prompt.isDefault = false;
                            }
                        });
                    }
                } else {
                    console.error("Failed to normalize prompt or missing guid:", action.payload);
                }
            })
            .addCase(createSystemPrompt.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to create system prompt';
            })

            // Update system prompt
            .addCase(updateSystemPrompt.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(updateSystemPrompt.fulfilled, (state, action) => {
                state.loading = false;
                const updatedPrompt = normalizePrompt(action.payload);

                if (updatedPrompt && updatedPrompt.guid) {
                    const index = state.prompts.findIndex(p => p.guid === updatedPrompt.guid);
                    if (index !== -1) {
                        state.prompts[index] = updatedPrompt;
                    }
                    state.currentPrompt = updatedPrompt;

                    if (updatedPrompt.isDefault) {
                        state.defaultPromptId = updatedPrompt.guid;
                        // Update other prompts to not be default
                        state.prompts.forEach(prompt => {
                            if (prompt.guid !== updatedPrompt.guid) {
                                prompt.isDefault = false;
                            }
                        });
                    }
                }
            })
            .addCase(updateSystemPrompt.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to update system prompt';
            })

            // Delete system prompt
            .addCase(deleteSystemPrompt.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(deleteSystemPrompt.fulfilled, (state, action) => {
                state.loading = false;
                state.prompts = state.prompts.filter(p => p.guid !== action.payload);
                if (state.currentPrompt?.guid === action.payload) {
                    state.currentPrompt = null;
                }
                if (state.defaultPromptId === action.payload) {
                    // Find a new default prompt if available
                    state.defaultPromptId = state.prompts.length > 0 ? state.prompts[0].guid : null;
                }
                // Remove from conversation mappings
                Object.keys(state.conversationPrompts).forEach(convId => {
                    if (state.conversationPrompts[convId] === action.payload) {
                        delete state.conversationPrompts[convId];
                    }
                });
            })
            .addCase(deleteSystemPrompt.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to delete system prompt';
            })

            // Set default system prompt
            .addCase(setDefaultSystemPrompt.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(setDefaultSystemPrompt.fulfilled, (state, action) => {
                state.loading = false;
                state.defaultPromptId = action.payload;
                // Update isDefault flag on all prompts
                state.prompts.forEach(prompt => {
                    prompt.isDefault = prompt.guid === action.payload;
                });
            })
            .addCase(setDefaultSystemPrompt.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to set default system prompt';
            })

            // Get conversation system prompt
            .addCase(getConversationSystemPrompt.fulfilled, (state, action) => {
                if (action.payload.prompt) {
                    state.conversationPrompts[action.payload.conversationId] = action.payload.prompt.guid;
                    // Make sure the prompt is in our list
                    const exists = state.prompts.some(p => p.guid === action.payload.prompt.guid);
                    if (!exists) {
                        state.prompts.push(action.payload.prompt);
                    }
                }
            })

            // Set conversation system prompt
            .addCase(setConversationSystemPrompt.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(setConversationSystemPrompt.fulfilled, (state, action) => {
                state.loading = false;
                console.log(`Setting conversation ${action.payload.conversationId} prompt to ${action.payload.promptId}`);
                state.conversationPrompts[action.payload.conversationId] = action.payload.promptId;
            })
            .addCase(setConversationSystemPrompt.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || 'Failed to set conversation system prompt';
                console.error("Error setting conversation prompt:", action.error);
            })

            // Clear conversation system prompt
            .addCase(clearConversationSystemPrompt.fulfilled, (state, action) => {
                delete state.conversationPrompts[action.payload];
            });
    },
});

export const { setCurrentPrompt, toggleLibrary, clearError } = systemPromptSlice.actions;
export default systemPromptSlice.reducer;