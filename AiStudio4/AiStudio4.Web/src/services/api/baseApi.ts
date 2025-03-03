// src/services/api/baseApi.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

// Create the base API with shared configuration
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({ 
    baseUrl: '/',
    prepareHeaders: (headers) => {
      // Add client ID to all requests
      const clientId = localStorage.getItem('clientId');
      if (clientId) {
        headers.set('X-Client-Id', clientId);
      }
      return headers;
    },
  }),
  tagTypes: ['Conversations', 'Tools', 'ToolCategories', 'SystemPrompts', 'Models', 'ServiceProviders'],
  endpoints: () => ({}),
});
