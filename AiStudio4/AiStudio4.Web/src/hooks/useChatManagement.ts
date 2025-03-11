// src/hooks/useChatManagement.ts
import { useCallback, useRef } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useConversationStore } from '@/stores/useConversationStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useHistoricalConversationsStore } from '@/stores/useHistoricalConversationsStore';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

interface SendMessageParams {
  conversationId: string;
  parentMessageId: string;
  message: string;
  model: string;
  toolIds: string[];
  systemPromptId?: string;
  systemPromptContent?: string;
}

export function useChatManagement() {
  // Use API call state utility
  const { 
    isLoading, 
    error, 
    executeApiCall, 
    clearError 
  } = useApiCallState();
  
  // Access Zustand stores
  const { 
    addMessage, 
    createConversation, 
    activeConversationId,
    conversations
  } = useConversationStore();
  
  const { 
    prompts, 
    conversationPrompts, 
    defaultPromptId 
  } = useSystemPromptStore();

  // Access Historical Conversations Store
  const {
    fetchConversationTree
  } = useHistoricalConversationsStore();

  // Send a chat message
  const sendMessage = useCallback(async (params: SendMessageParams) => {
    return executeApiCall(async () => {
      // Add a unique ID for the new message
      const newMessageId = params.parentMessageId ? uuidv4() : undefined;
      
      const sendMessageRequest = createApiRequest('/api/chat', 'POST');
      const data = await sendMessageRequest({
        ...params,
        newMessageId
      });
      
      return { 
        messageId: data.messageId, 
        success: true 
      };
    });
  }, []);
  
  // Get configuration
  const getConfig = useCallback(async () => {
    return executeApiCall(async () => {
      const getConfigRequest = createApiRequest('/api/getConfig', 'POST');
      const data = await getConfigRequest({});
      
      return {
        models: data.models || [],
        defaultModel: data.defaultModel || '',
        secondaryModel: data.secondaryModel || ''
      };
    }) || {
      models: [],
      defaultModel: '',
      secondaryModel: ''
    };
  }, []);
  
  // Set default model
  const setDefaultModel = useCallback(async (modelName: string) => {
    return executeApiCall(async () => {
      const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
      await setDefaultModelRequest({ modelName });
      return true;
    }) || false;
  }, []);
  
  // Set secondary model
  const setSecondaryModel = useCallback(async (modelName: string) => {
    return executeApiCall(async () => {
      const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
      await setSecondaryModelRequest({ modelName });
      return true;
    }) || false;
  }, []);
  
  // Get conversation history - first check Zustand store, then use the historical conversations store
  const getConversation = useCallback(async (conversationId: string) => {
    // First check if we already have this conversation in the Zustand store
    const localConversation = conversations[conversationId];
    if (localConversation) {
      return {
        id: conversationId,
        messages: localConversation.messages
      };
    }

    // If not in local store, use the historical conversations store to fetch it
    return executeApiCall(async () => {
      // Use the fetchConversationTree function from the historical conversations store
      const treeData = await fetchConversationTree(conversationId);

      if (!treeData) {
        throw new Error('Failed to get conversation tree');
      }

      // Convert the tree data to the format expected by the chat management
      // We need to extract all nodes from the tree in a flat structure
      const extractNodes = (node: any, nodes: any[] = []) => {
        if (!node) return nodes;

        nodes.push({
          id: node.id,
          text: node.text,
          parentId: node.parentId,
          tokenUsage: node.tokenUsage,
          source: node.source
        });

        if (node.children && Array.isArray(node.children)) {
          for (const child of node.children) {
            extractNodes(child, nodes);
          }
        }

        return nodes;
      };

      const flatNodes = extractNodes(treeData);

      // Map the flat nodes to the message format needed by the conversation
      const messages = flatNodes.map(node => ({
        id: node.id,
        content: node.text,
        source: node.source ||
          (node.id.includes('user') ? 'user' :
            node.id.includes('ai') || node.id.includes('msg') ? 'ai' : 'system'),
        parentId: node.parentId,
        timestamp: Date.now(), // No timestamp in tree data, use current time
        tokenUsage: node.tokenUsage || null,
        costInfo: node.costInfo || null
      }));

      return {
        id: conversationId,
        messages: messages,
        summary: 'Loaded Conversation' // We might need to get this from another source
      };
    });
  }, [conversations, fetchConversationTree]);
  
  // Helper method to determine system prompt for a conversation
  const getSystemPromptForConversation = useCallback((conversationId: string) => {
    // Check if conversation has a specific prompt assigned
    let promptId = conversationId ? conversationPrompts[conversationId] : null;
    
    // If no conversation-specific prompt, use default
    if (!promptId) {
      promptId = defaultPromptId;
    }
    
    // Find the prompt
    if (promptId) {
      const prompt = prompts.find(p => p.guid === promptId);
      if (prompt) {
        return {
          id: prompt.guid,
          content: prompt.content
        };
      }
    }
    
    // Find any prompt marked as default
    const defaultPrompt = prompts.find(p => p.isDefault);
    if (defaultPrompt) {
      return {
        id: defaultPrompt.guid,
        content: defaultPrompt.content
      };
    }
    
    // No prompt found
    return null;
  }, [prompts, conversationPrompts, defaultPromptId]);
  
  return {
    // State
    isLoading,
    error,
    activeConversationId,
    conversations,
    
    // Actions
    sendMessage,
    getConfig,
    setDefaultModel,
    setSecondaryModel,
    getConversation,
    getSystemPromptForConversation,
    clearError
  };
}