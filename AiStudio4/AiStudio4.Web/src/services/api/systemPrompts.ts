// src/services/api/systemPrompts.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { SystemPrompt } from '@/types/systemPromptTypes';
import { apiClient } from './apiClient';

// API slice for system prompt-related endpoints
export const systemPromptsApi = createApi({
  reducerPath: 'systemPromptsApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/' }),
  tagTypes: ['SystemPrompts'],
  endpoints: (builder) => ({
    // Get all system prompts
    getSystemPrompts: builder.query<SystemPrompt[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getSystemPrompts', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to fetch system prompts');
          }
          return { data: response.data.prompts || [] };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['SystemPrompts']
    }),
    
    // Add a system prompt
    addSystemPrompt: builder.mutation<SystemPrompt, Omit<SystemPrompt, 'guid'>>({ 
      queryFn: async (prompt) => {
        try {
          const response = await apiClient.post('/api/addSystemPrompt', prompt);
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to add system prompt');
          }
          return { data: response.data.prompt };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['SystemPrompts']
    }),
    
    // Update a system prompt
    updateSystemPrompt: builder.mutation<SystemPrompt, SystemPrompt>({
      queryFn: async (prompt) => {
        try {
          const response = await apiClient.post('/api/updateSystemPrompt', prompt);
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to update system prompt');
          }
          return { data: response.data.prompt };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['SystemPrompts']
    }),
    
    // Delete a system prompt
    deleteSystemPrompt: builder.mutation<{ success: boolean }, string>({
      queryFn: async (promptId) => {
        try {
          const response = await apiClient.post('/api/deleteSystemPrompt', { promptId });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to delete system prompt');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['SystemPrompts']
    }),
    
    // Set conversation system prompt
    setConversationSystemPrompt: builder.mutation<{ success: boolean }, { conversationId: string, promptId: string }>({
      queryFn: async ({ conversationId, promptId }) => {
        try {
          const response = await apiClient.post('/api/setConversationSystemPrompt', { conversationId, promptId });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to set conversation system prompt');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      }
    }),
    
    // Import system prompts
    importSystemPrompts: builder.mutation<{ success: boolean }, string>({
      queryFn: async (jsonData) => {
        try {
          const response = await apiClient.post('/api/importSystemPrompts', { jsonData });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to import system prompts');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['SystemPrompts']
    }),
    
    // Export system prompts
    exportSystemPrompts: builder.mutation<string, void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/exportSystemPrompts', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to export system prompts');
          }
          return { data: response.data.json };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      }
    }),
  }),
});

// Export hooks
export const {
  useGetSystemPromptsQuery,
  useAddSystemPromptMutation,
  useUpdateSystemPromptMutation,
  useDeleteSystemPromptMutation,
  useSetConversationSystemPromptMutation,
  useImportSystemPromptsMutation,
  useExportSystemPromptsMutation,
} = systemPromptsApi;
