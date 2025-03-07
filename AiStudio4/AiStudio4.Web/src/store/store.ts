import { configureStore } from '@reduxjs/toolkit';
import toolReducer from './toolSlice';
import systemPromptReducer from './systemPromptSlice';
import pinnedCommandsReducer from './pinnedCommandsSlice';
import { baseApi } from '@/services/api/baseApi';

// Initialize the client ID if it doesn't exist
if (!localStorage.getItem('clientId')) {
    const clientId = `client_${Math.random().toString(36).substring(2, 11)}`;
    localStorage.setItem('clientId', clientId);
    console.log('Generated and stored new client ID:', clientId);
}

export const store = configureStore({
    reducer: {
        // conversationReducer has been removed - now using Zustand
        tools: toolReducer,
        systemPrompts: systemPromptReducer,
        pinnedCommands: pinnedCommandsReducer,
        [baseApi.reducerPath]: baseApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
            serializableCheck: {
                // Ignore these paths in the state
                ignoredPaths: ['pinnedCommands.pinnedCommands'],
                // Ignore these action types
                ignoredActions: ['pinnedCommands/addPinnedCommand'],
            },
        }).concat(baseApi.middleware),
});

// For debugging in browser console
(window as any).store = store;

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;