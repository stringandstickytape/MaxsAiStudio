// src/hooks/useChatManagement.ts
import { useState, useCallback, useEffect, useRef } from 'react';
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
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
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
    try {
      setIsLoading(true);
      setError(null);
      
      // Add a unique ID for the new message
      const newMessageId = params.parentMessageId ? uuidv4() : undefined;
      
      // Use direct API call
      const response = await apiClient.post('/api/chat', {
        ...params,
        newMessageId
      });
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to send message');
      }
      
      return { 
        messageId: data.messageId, 
        success: true 
      };
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error sending message';
      setError(errMsg);
      console.error('Error sending message:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Get configuration
  const getConfig = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/getConfig', {});
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to get configuration');
      }
      
      return {
        models: data.models || [],
        defaultModel: data.defaultModel || '',
        secondaryModel: data.secondaryModel || ''
      };
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error getting configuration';
      setError(errMsg);
      console.error('Error getting configuration:', err);
      return {
        models: [],
        defaultModel: '',
        secondaryModel: ''
      };
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Set default model
  const setDefaultModel = useCallback(async (modelName: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/setDefaultModel', { modelName });
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set default model');
      }
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error setting default model';
      setError(errMsg);
      console.error('Error setting default model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Set secondary model
  const setSecondaryModel = useCallback(async (modelName: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiClient.post('/api/setSecondaryModel', { modelName });
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set secondary model');
      }
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error setting secondary model';
      setError(errMsg);
      console.error('Error setting secondary model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
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
    try {
      setIsLoading(true);
      setError(null);

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
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error getting conversation';
      setError(errMsg);
      console.error('Error getting conversation:', err);
      return null;
    } finally {
      setIsLoading(false);
    }
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
    clearError: () => setError(null)
  };
}