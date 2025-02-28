import { configureStore } from '@reduxjs/toolkit';
import conversationReducer from './conversationSlice';

export const store = configureStore({
    reducer: {
        conversations: conversationReducer,
    },
});
(window as any).store = store;
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;