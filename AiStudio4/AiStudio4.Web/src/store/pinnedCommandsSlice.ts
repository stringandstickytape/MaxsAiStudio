// src/store/pinnedCommandsSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';

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
}

const initialState: PinnedCommandsState = {
    pinnedCommands: []
};

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
        }
    }
});

export const { addPinnedCommand, removePinnedCommand, reorderPinnedCommands } = pinnedCommandsSlice.actions;
export default pinnedCommandsSlice.reducer;

// Debug helper - expose pinned commands state
export const debugPinnedCommands = () => {
    const state = (window as any).store.getState().pinnedCommands;
    console.group('Pinned Commands Debug');
    console.log('Count:', state.pinnedCommands.length);
    console.log('Commands:', state.pinnedCommands);
    console.groupEnd();
    return state;
};

// Export for console access
(window as any).debugPinnedCommands = debugPinnedCommands;