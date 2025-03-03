// src/store/toolSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Tool, ToolCategory } from '@/types/toolTypes';

interface ToolState {
    tools: Tool[];
    categories: ToolCategory[];
    activeTools: string[];
    loading: boolean;
    error: string | null;
}

const initialState: ToolState = {
    tools: [],
    categories: [],
    activeTools: [],
    loading: false,
    error: null,
};

const toolSlice = createSlice({
    name: 'tools',
    initialState,
    reducers: {
        setActiveTools: (state, action: PayloadAction<string[]>) => {
            state.activeTools = action.payload;
        },
        addActiveTool: (state, action: PayloadAction<string>) => {
            if (!state.activeTools.includes(action.payload)) {
                state.activeTools.push(action.payload);
            }
        },
        removeActiveTool: (state, action: PayloadAction<string>) => {
            state.activeTools = state.activeTools.filter(id => id !== action.payload);
        },
        clearActiveTools: (state) => {
            state.activeTools = [];
        },
        // Add these actions to sync with the RTK Query state if needed
        setTools: (state, action: PayloadAction<Tool[]>) => {
            state.tools = action.payload;
        },
        setCategories: (state, action: PayloadAction<ToolCategory[]>) => {
            state.categories = action.payload;
        },
    },
});

export const {
    setActiveTools,
    addActiveTool,
    removeActiveTool,
    clearActiveTools,
    setTools,
    setCategories
} = toolSlice.actions;

export default toolSlice.reducer;