import { configureStore } from '@reduxjs/toolkit';
import conversationReducer from './conversationSlice';
import toolReducer from './toolSlice';

export const store = configureStore({
    reducer: {
        conversations: conversationReducer,
        tools: toolReducer,
    },
});
(window as any).store = store;
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;