// src/store/store.ts
import { baseApi } from '@/services/api/baseApi';
import { configureStore } from '@reduxjs/toolkit';

// Initialize the client ID if it doesn't exist
if (!localStorage.getItem('clientId')) {
    const clientId = `client_${Math.random().toString(36).substring(2, 11)}`;
    localStorage.setItem('clientId', clientId);
    console.log('Generated and stored new client ID:', clientId);
}

// We're only keeping the Redux store for RTK Query
// All other state management has been migrated to Zustand
export const store = configureStore({
    reducer: {
        [baseApi.reducerPath]: baseApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(baseApi.middleware),
});

// For debugging in browser console
(window as any).store = store;

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;