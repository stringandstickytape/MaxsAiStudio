// src/services/api/systemPromptApi.ts
import { baseApi } from './baseApi';
import { SystemPrompt } from '@/types/systemPrompt';

interface SystemPromptResponse {
  success: boolean;
  prompts?: SystemPrompt[];
  prompt?: SystemPrompt;
  error?: string;
}

type SystemPromptCreateData = Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>;

export const systemPromptApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getSystemPrompts: builder.query<SystemPrompt[], void>({
      query: () => ({
        url: '/api/getSystemPrompts',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: SystemPromptResponse) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch system prompts');
        }
        return response.prompts || [];
      },
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ guid }) => ({ type: 'SystemPrompts' as const, id: guid })),
              { type: 'SystemPrompts', id: 'LIST' },
            ]
          : [{ type: 'SystemPrompts', id: 'LIST' }],
    }),
    
    getSystemPromptById: builder.query<SystemPrompt, string>({
      query: (promptId) => ({
        url: '/api/getSystemPrompt',
        method: 'POST',
        body: { promptId },
      }),
      transformResponse: (response: SystemPromptResponse) => {
        if (!response.success || !response.prompt) {
          throw new Error(response.error || 'Failed to fetch system prompt');
        }
        return response.prompt;
      },
      providesTags: (result, error, id) => [{ type: 'SystemPrompts', id }],
    }),
    
    createSystemPrompt: builder.mutation<SystemPrompt, SystemPromptCreateData>({
      query: (promptData) => ({
        url: '/api/createSystemPrompt',
        method: 'POST',
        body: promptData,
      }),
      transformResponse: (response: SystemPromptResponse) => {
        if (!response.success || !response.prompt) {
          throw new Error(response.error || 'Failed to create system prompt');
        }
        return response.prompt;
      },
      invalidatesTags: [{ type: 'SystemPrompts', id: 'LIST' }],
    }),
    
    updateSystemPrompt: builder.mutation<SystemPrompt, SystemPrompt>({
      query: (promptData) => ({
        url: '/api/updateSystemPrompt',
        method: 'POST',
        body: promptData,
      }),
      transformResponse: (response: SystemPromptResponse) => {
        if (!response.success || !response.prompt) {
          throw new Error(response.error || 'Failed to update system prompt');
        }
        return response.prompt;
      },
      invalidatesTags: (result, error, arg) => [{ type: 'SystemPrompts', id: arg.guid }],
    }),
    
    deleteSystemPrompt: builder.mutation<boolean, string>({
      query: (promptId) => ({
        url: '/api/deleteSystemPrompt',
        method: 'POST',
        body: { promptId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to delete system prompt');
        }
        return true;
      },
      invalidatesTags: (result, error, id) => [
        { type: 'SystemPrompts', id },
        { type: 'SystemPrompts', id: 'LIST' },
      ],
    }),
    
    setDefaultSystemPrompt: builder.mutation<boolean, string>({
      query: (promptId) => ({
        url: '/api/setDefaultSystemPrompt',
        method: 'POST',
        body: { promptId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to set default system prompt');
        }
        return true;
      },
      invalidatesTags: ['SystemPrompts'],
    }),
    
    getConversationSystemPrompt: builder.query<SystemPrompt | null, string>({
      query: (conversationId) => ({
        url: '/api/getConversationSystemPrompt',
        method: 'POST',
        body: { conversationId },
      }),
      transformResponse: (response: SystemPromptResponse) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to fetch conversation system prompt');
        }
        return response.prompt || null;
      },
      providesTags: (result, error, conversationId) => [
        { type: 'SystemPrompts', id: `conversation-${conversationId}` },
      ],
    }),
    
    setConversationSystemPrompt: builder.mutation<boolean, { conversationId: string; promptId: string }>({
      query: ({ conversationId, promptId }) => ({
        url: '/api/setConversationSystemPrompt',
        method: 'POST',
        body: { conversationId, promptId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to set conversation system prompt');
        }
        return true;
      },
      invalidatesTags: (result, error, { conversationId }) => [
        { type: 'SystemPrompts', id: `conversation-${conversationId}` },
      ],
    }),
    
    clearConversationSystemPrompt: builder.mutation<boolean, string>({
      query: (conversationId) => ({
        url: '/api/clearConversationSystemPrompt',
        method: 'POST',
        body: { conversationId },
      }),
      transformResponse: (response: { success: boolean; error?: string }) => {
        if (!response.success) {
          throw new Error(response.error || 'Failed to clear conversation system prompt');
        }
        return true;
      },
      invalidatesTags: (result, error, conversationId) => [
        { type: 'SystemPrompts', id: `conversation-${conversationId}` },
      ],
    }),
  }),
});

export const {
  useGetSystemPromptsQuery,
  useGetSystemPromptByIdQuery,
  useCreateSystemPromptMutation,
  useUpdateSystemPromptMutation,
  useDeleteSystemPromptMutation,
  useSetDefaultSystemPromptMutation,
  useGetConversationSystemPromptQuery,
  useSetConversationSystemPromptMutation,
  useClearConversationSystemPromptMutation,
} = systemPromptApi;
