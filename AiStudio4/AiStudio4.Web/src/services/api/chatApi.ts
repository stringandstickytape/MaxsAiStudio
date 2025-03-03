// src/services/api/chatApi.ts
import { baseApi } from './baseApi';
import { v4 as uuidv4 } from 'uuid';
import { Message } from '@/types/conversation';

interface SendMessageRequest {
  conversationId: string;
  parentMessageId: string;
  message: string;
  model: string;
  toolIds: string[];
  systemPromptId?: string;
  systemPromptContent?: string;
}

interface SendMessageResponse {
  success: boolean;
  messageId: string;
  error?: string;
}

interface ModelConfig {
  models: string[];
  defaultModel: string;
  secondaryModel: string;
}

interface SetModelRequest {
  modelName: string;
}

export const chatApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    sendMessage: builder.mutation<SendMessageResponse, SendMessageRequest>({
      query: (messageData) => ({
        url: '/api/chat',
        method: 'POST',
        body: {
          ...messageData,
          newMessageId: messageData.parentMessageId ? uuidv4() : undefined,
        },
      }),
      invalidatesTags: ['Conversations'],
    }),
    
    getConfig: builder.query<ModelConfig, void>({
      query: () => ({
        url: '/api/getConfig',
        method: 'POST',
        body: {},
      }),
      transformResponse: (response: { success: boolean; models: string[]; defaultModel: string; secondaryModel: string }) => ({
        models: response.models || [],
        defaultModel: response.defaultModel || "",
        secondaryModel: response.secondaryModel || ""
      }),
    }),
    
    setDefaultModel: builder.mutation<{ success: boolean }, SetModelRequest>({
      query: (data) => ({
        url: '/api/setDefaultModel',
        method: 'POST',
        body: data,
      }),
    }),
    
    setSecondaryModel: builder.mutation<{ success: boolean }, SetModelRequest>({
      query: (data) => ({
        url: '/api/setSecondaryModel',
        method: 'POST',
        body: data,
      }),
    }),
  }),
});

export const {
  useSendMessageMutation,
  useGetConfigQuery,
  useSetDefaultModelMutation,
  useSetSecondaryModelMutation,
} = chatApi;
