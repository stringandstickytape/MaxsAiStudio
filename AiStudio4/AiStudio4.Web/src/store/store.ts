import { configureStore } from '@reduxjs/toolkit';
import conversationReducer from './conversationSlice';
import toolReducer from './toolSlice';
import systemPromptReducer from './systemPromptSlice';
import pinnedCommandsReducer from './pinnedCommandsSlice';
import { baseApi } from '@/services/api/baseApi';

export const store = configureStore({
    reducer: {
        conversations: conversationReducer,
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