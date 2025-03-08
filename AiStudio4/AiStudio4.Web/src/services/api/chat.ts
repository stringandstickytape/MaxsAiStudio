// src/services/api/chat.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { apiClient } from './apiClient';

// API slice for chat-related endpoints
export const chatApi = createApi({
  reducerPath: 'chatApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/' }),
  tagTypes: ['Config', 'Conversations'],
  endpoints: (builder) => ({
    // Get configuration
    getConfig: builder.query<any, void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getConfig', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to fetch configuration');
          }
          return { data: response.data };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['Config']
    }),
    
    // Set default model
    setDefaultModel: builder.mutation<{ success: boolean }, { modelName: string }>({
      queryFn: async ({ modelName }) => {
        try {
          const response = await apiClient.post('/api/setDefaultModel', { modelName });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to set default model');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Config']
    }),
    
    // Set secondary model
    setSecondaryModel: builder.mutation<{ success: boolean }, { modelName: string }>({
      queryFn: async ({ modelName }) => {
        try {
          const response = await apiClient.post('/api/setSecondaryModel', { modelName });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to set secondary model');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Config']
    }),
    
    // Get conversations
    getConversations: builder.query<any[], void>({
      queryFn: async () => {
        try {
          const response = await apiClient.post('/api/getConversations', {});
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to fetch conversations');
          }
          return { data: response.data.conversations || [] };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      providesTags: ['Conversations']
    }),
    
    // Create conversation
    createConversation: builder.mutation<any, { title?: string }>({ 
      queryFn: async (params) => {
        try {
          const response = await apiClient.post('/api/createConversation', params);
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to create conversation');
          }
          return { data: response.data.conversation };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Conversations']
    }),
    
    // Delete conversation
    deleteConversation: builder.mutation<{ success: boolean }, string>({
      queryFn: async (conversationId) => {
        try {
          const response = await apiClient.post('/api/deleteConversation', { conversationId });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to delete conversation');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Conversations']
    }),
    
    // Rename conversation
    renameConversation: builder.mutation<{ success: boolean }, { conversationId: string, title: string }>({
      queryFn: async ({ conversationId, title }) => {
        try {
          const response = await apiClient.post('/api/renameConversation', { conversationId, title });
          if (!response.data.success) {
            throw new Error(response.data.error || 'Failed to rename conversation');
          }
          return { data: { success: true } };
        } catch (error) {
          return { error: { status: 'CUSTOM_ERROR', data: error } };
        }
      },
      invalidatesTags: ['Conversations']
    }),
  }),
});

// Export hooks
export const {
  useGetConfigQuery,
  useSetDefaultModelMutation,
  useSetSecondaryModelMutation,
  useGetConversationsQuery,
  useCreateConversationMutation,
  useDeleteConversationMutation,
  useRenameConversationMutation,
} = chatApi;
