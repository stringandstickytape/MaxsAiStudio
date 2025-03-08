// src/hooks/useChatManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useConversationStore } from '@/stores/useConversationStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { v4 as uuidv4 } from 'uuid';

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
    activeConversationId 
  } = useConversationStore();
  
  const { 
    prompts, 
    conversationPrompts, 
    defaultPromptId 
  } = useSystemPromptStore();

  // Send a chat message
  const sendMessage = useCallback(async (params: SendMessageParams) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      // Add a unique ID for the new message
      const newMessageId = params.parentMessageId ? uuidv4() : undefined;
      
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({
          ...params,
          newMessageId
        })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to send message');
      }
      
      return { messageId: data.messageId, success: true };
    } catch (err) {
      setError(`Failed to send message: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/getConfig', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({})
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to get configuration');
      }
      
      return {
        models: data.models || [],
        defaultModel: data.defaultModel || '',
        secondaryModel: data.secondaryModel || ''
      };
    } catch (err) {
      setError(`Failed to get configuration: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/setDefaultModel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ modelName })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set default model');
      }
      
      return true;
    } catch (err) {
      setError(`Failed to set default model: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/setSecondaryModel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ modelName })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set secondary model');
      }
      
      return true;
    } catch (err) {
      setError(`Failed to set secondary model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error setting secondary model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Get conversation history
  const getConversation = useCallback(async (conversationId: string) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/getConversation', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ conversationId })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to get conversation');
      }
      
      return data.conversation;
    } catch (err) {
      setError(`Failed to get conversation: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error getting conversation:', err);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
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