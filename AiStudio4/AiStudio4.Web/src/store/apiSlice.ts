// src/store/apiSlice.ts
import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { chatApi } from '@/services/api/chat';
import { toolsApi } from '@/services/api/tools';
import { systemPromptsApi } from '@/services/api/systemPrompts';
import { modelsApi } from '@/services/api/models';

// Configure the store with all API reducers
export const apiStore = configureStore({
  reducer: {
    [chatApi.reducerPath]: chatApi.reducer,
    [toolsApi.reducerPath]: toolsApi.reducer,
    [systemPromptsApi.reducerPath]: systemPromptsApi.reducer,
    [modelsApi.reducerPath]: modelsApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware()
      .concat(chatApi.middleware)
      .concat(toolsApi.middleware)
      .concat(systemPromptsApi.middleware)
      .concat(modelsApi.middleware),
});

// Setup listeners for automatic refetching
setupListeners(apiStore.dispatch);

// Export types
export type RootState = ReturnType<typeof apiStore.getState>;
export type AppDispatch = typeof apiStore.dispatch;
