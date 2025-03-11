// src/hooks/useChatManagement.ts
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { v4 as uuidv4 } from 'uuid';
import { createResourceHook } from './useResourceFactory';

// Create a resource hook for chat configuration
const useChatConfigResource = createResourceHook<{
  models: string[];
  defaultModel: string;
  secondaryModel: string;
}>({
  endpoints: {
    fetch: '/api/getConfig'
  },
  storeActions: {
    setItems: () => {} // No direct store action for config
  },
  options: {
    transformFetchResponse: (data) => [{
      models: data.models || [],
      defaultModel: data.defaultModel || '',
      secondaryModel: data.secondaryModel || ''
    }]
  }
});

interface SendMessageParams {
  convId: string;
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
  
  // Use the config resource hook
  const {
    fetchItems: fetchConfigData
  } = useChatConfigResource();
  
  // Access Zustand stores
  const { 
    addMessage, 
    createConv, 
    activeConvId,
    convs
  } = useConvStore();
  
  const { 
    prompts, 
    convPrompts, 
    defaultPromptId 
  } = useSystemPromptStore();

  // Access Historical Convs Store
  const {
    fetchConvTree
  } = useHistoricalConvsStore();

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
  }, [executeApiCall]);
  
  // Get configuration
  const getConfig = useCallback(async () => {
    const config = await fetchConfigData();
    return config?.[0] || {
      models: [],
      defaultModel: '',
      secondaryModel: ''
    };
  }, [fetchConfigData]);
  
  // Set default model
  const setDefaultModel = useCallback(async (modelName: string) => {
    return executeApiCall(async () => {
      const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
      await setDefaultModelRequest({ modelName });
      return true;
    }) || false;
  }, [executeApiCall]);
  
  // Set secondary model
  const setSecondaryModel = useCallback(async (modelName: string) => {
    return executeApiCall(async () => {
      const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
      await setSecondaryModelRequest({ modelName });
      return true;
    }) || false;
  }, [executeApiCall]);
  
  // Get conv history - first check Zustand store, then use the historical convs store
  const getConv = useCallback(async (convId: string) => {
    // First check if we already have this conv in the Zustand store
    const localConv = convs[convId];
    if (localConv) {
      return {
        id: convId,
        messages: localConv.messages
      };
    }

    // If not in local store, use the historical convs store to fetch it
    return executeApiCall(async () => {
      // Use the fetchConvTree function from the historical convs store
      const treeData = await fetchConvTree(convId);

      if (!treeData) {
        throw new Error('Failed to get conv tree');
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

      // Map the flat nodes to the message format needed by the conv
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
        id: convId,
        messages: messages,
        summary: 'Loaded Conv' // We might need to get this from another source
      };
    }, convs, fetchConvTree);
  }, [convs, fetchConvTree, executeApiCall]);
  
  // Helper method to determine system prompt for a conv
  const getSystemPromptForConv = useCallback((convId: string) => {
    // Check if conv has a specific prompt assigned
    let promptId = convId ? convPrompts[convId] : null;
    
    // If no conv-specific prompt, use default
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
  }, [prompts, convPrompts, defaultPromptId]);
  
  return {
    // State
    isLoading,
    error,
    activeConvId,
    convs,
    
    // Actions
    sendMessage,
    getConfig,
    setDefaultModel,
    setSecondaryModel,
    getConv,
    getSystemPromptForConv,
    clearError
  };
}
