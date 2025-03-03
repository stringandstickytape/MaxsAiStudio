import { configureStore } from '@reduxjs/toolkit';
import conversationReducer from './conversationSlice';
import toolReducer from './toolSlice';
import systemPromptReducer from './systemPromptSlice';
import { baseApi } from '@/services/api/baseApi';

export const store = configureStore({
    reducer: {
        conversations: conversationReducer,
        tools: toolReducer,
        systemPrompts: systemPromptReducer,
        [baseApi.reducerPath]: baseApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(baseApi.middleware),
});

// For debugging in browser console
(window as any).store = store;

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;