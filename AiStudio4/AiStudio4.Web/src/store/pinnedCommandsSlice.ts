// src/store/pinnedCommandsSlice.ts
import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit';

// Import command type for strong typing
import { Command } from '@/commands/types';

export interface PinnedCommand {
    id: string;
    name: string;
    iconName?: string; // Store icon name instead of React component
    section: string;
}

interface PinnedCommandsState {
    pinnedCommands: PinnedCommand[];
    loading: boolean;
    error: string | null;
}

const initialState: PinnedCommandsState = {
    pinnedCommands: [],
    loading: false,
    error: null
};

// Define async thunks for API interactions
export const fetchPinnedCommands = createAsyncThunk(
    'pinnedCommands/fetchPinnedCommands',
    async (_, { rejectWithValue }) => {
        try {
            const response = await fetch('/api/pinnedCommands/get', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || ''
                },
                body: JSON.stringify({})
            });

            const data = await response.json();

            if (!data.success) {
                return rejectWithValue(data.error || 'Failed to fetch pinned commands');
            }

            return data.pinnedCommands;
        } catch (error) {
            return rejectWithValue(error instanceof Error ? error.message : 'Unknown error');
        }
    }
);

export const savePinnedCommands = createAsyncThunk(
    'pinnedCommands/savePinnedCommands',
    async (pinnedCommands: PinnedCommand[], { rejectWithValue }) => {
        try {
            const response = await fetch('/api/pinnedCommands/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || ''
                },
                body: JSON.stringify({ pinnedCommands })
            });

            const data = await response.json();

            if (!data.success) {
                return rejectWithValue(data.error || 'Failed to save pinned commands');
            }

            return pinnedCommands;
        } catch (error) {
            return rejectWithValue(error instanceof Error ? error.message : 'Unknown error');
        }
    }
);

const pinnedCommandsSlice = createSlice({
    name: 'pinnedCommands',
    initialState,
    reducers: {
        addPinnedCommand: (state, action: PayloadAction<PinnedCommand>) => {
            if (!state.pinnedCommands.some(cmd => cmd.id === action.payload.id)) {
                state.pinnedCommands.push(action.payload);
            }
        },
        removePinnedCommand: (state, action: PayloadAction<string>) => {
            state.pinnedCommands = state.pinnedCommands.filter(cmd => cmd.id !== action.payload);
        },
        reorderPinnedCommands: (state, action: PayloadAction<string[]>) => {
            const orderedCommands: PinnedCommand[] = [];
            action.payload.forEach(id => {
                const command = state.pinnedCommands.find(cmd => cmd.id === id);
                if (command) {
                    orderedCommands.push(command);
                }
            });
            state.pinnedCommands = orderedCommands;
        },
        setPinnedCommands: (state, action: PayloadAction<PinnedCommand[]>) => {
            state.pinnedCommands = action.payload;
        }
    },
    extraReducers: (builder) => {
        // Handle loading pinned commands
        builder.addCase(fetchPinnedCommands.pending, (state) => {
            state.loading = true;
            state.error = null;
        });
        builder.addCase(fetchPinnedCommands.fulfilled, (state, action) => {
            state.loading = false;
            state.pinnedCommands = action.payload;
        });
        builder.addCase(fetchPinnedCommands.rejected, (state, action) => {
            state.loading = false;
            state.error = action.payload as string;
        });

        // Handle saving pinned commands
        builder.addCase(savePinnedCommands.pending, (state) => {
            state.loading = true;
            state.error = null;
        });
        builder.addCase(savePinnedCommands.fulfilled, (state) => {
            state.loading = false;
        });
        builder.addCase(savePinnedCommands.rejected, (state, action) => {
            state.loading = false;
            state.error = action.payload as string;
        });
    }
});

export const { addPinnedCommand, removePinnedCommand, reorderPinnedCommands, setPinnedCommands } = pinnedCommandsSlice.actions;
export default pinnedCommandsSlice.reducer;

// Debug helper - expose pinned commands state
export const debugPinnedCommands = () => {
    const state = (window as any).store.getState().pinnedCommands;
    console.group('Pinned Commands Debug');
    console.log('Count:', state.pinnedCommands.length);
    console.log('Commands:', state.pinnedCommands);
    console.log('Loading:', state.loading);
    console.log('Error:', state.error);
    console.groupEnd();
    return state;
};

// Export for console access
(window as any).debugPinnedCommands = debugPinnedCommands;