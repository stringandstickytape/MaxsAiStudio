// src/hooks/useSystemPromptManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { v4 as uuidv4 } from 'uuid';

export function useSystemPromptManagement() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const { 
    prompts, 
    defaultPromptId,
    currentPrompt,
    setPrompts, 
    setConversationPrompt,
    setDefaultPromptId,
    setCurrentPrompt
  } = useSystemPromptStore();
  
  // Fetch all system prompts
  const fetchSystemPrompts = useCallback(async () => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      const response = await fetch('/api/getSystemPrompts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({})
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch system prompts');
      }
      
      setPrompts(data.prompts || []);
      return data.prompts;
    } catch (err) {
      setError(`Failed to fetch system prompts: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error fetching system prompts:', err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [setPrompts]);
  
  // Set a conversation's system prompt
  const setConversationSystemPrompt = useCallback(async (params: { 
    conversationId: string; 
    promptId: string;
  }) => {
    try {
      setIsLoading(true);
      const { conversationId, promptId } = params;
      const clientId = localStorage.getItem('clientId');
      
      // Update local state first for immediate UI feedback
      setConversationPrompt(conversationId, promptId);
      
      // Then update on the server
      const response = await fetch('/api/setConversationSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ conversationId, promptId })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set conversation system prompt');
      }
      
      return true;
    } catch (err) {
      setError(`Failed to set conversation system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error setting conversation system prompt:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [setConversationPrompt]);
  
  // Create a new system prompt
  const createSystemPrompt = useCallback(async (promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      // Create full prompt object
      const newPrompt = {
        ...promptData,
        guid: uuidv4(),
        createdDate: new Date().toISOString(),
        modifiedDate: new Date().toISOString()
      };
      
      const response = await fetch('/api/createSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify(newPrompt)
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to create system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      // Return the created prompt
      return data.prompt;
    } catch (err) {
      setError(`Failed to create system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error creating system prompt:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchSystemPrompts]);
  
  // Update an existing system prompt
  const updateSystemPrompt = useCallback(async (promptData: SystemPrompt) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      // Update modification date
      const updatedPrompt = {
        ...promptData,
        modifiedDate: new Date().toISOString()
      };
      
      const response = await fetch('/api/updateSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify(updatedPrompt)
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      // Return the updated prompt
      return data.prompt;
    } catch (err) {
      setError(`Failed to update system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error updating system prompt:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchSystemPrompts]);
  
  // Delete a system prompt
  const deleteSystemPrompt = useCallback(async (promptId: string) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/deleteSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ promptId })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to delete system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    } catch (err) {
      setError(`Failed to delete system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting system prompt:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchSystemPrompts]);
  
  // Set default system prompt
  const setDefaultSystemPrompt = useCallback(async (promptId: string) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      // Update local state first for immediate UI response
      setDefaultPromptId(promptId);
      
      const response = await fetch('/api/setDefaultSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ promptId })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set default system prompt');
      }
      
      return true;
    } catch (err) {
      setError(`Failed to set default system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error setting default system prompt:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [setDefaultPromptId]);
  
  // Get a specific prompt by ID
  const getSystemPromptById = useCallback(async (promptId: string) => {
    // First check in local state
    const localPrompt = prompts.find(p => p.guid === promptId);
    if (localPrompt) return localPrompt;
    
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/getSystemPrompt', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ promptId })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to get system prompt');
      }
      
      return data.prompt;
    } catch (err) {
      setError(`Failed to get system prompt: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error getting system prompt:', err);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [prompts]);

  // Import system prompts
  const importSystemPrompts = useCallback(async (jsonData: string) => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/importSystemPrompts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({ jsonData })
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to import system prompts');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    } catch (err) {
      setError(`Failed to import system prompts: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error importing system prompts:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchSystemPrompts]);

  // Export system prompts
  const exportSystemPrompts = useCallback(async () => {
    try {
      setIsLoading(true);
      const clientId = localStorage.getItem('clientId');
      
      const response = await fetch('/api/exportSystemPrompts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': clientId || ''
        },
        body: JSON.stringify({})
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to export system prompts');
      }
      
      return data.json;
    } catch (err) {
      setError(`Failed to export system prompts: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error exporting system prompts:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Initialize data on hook mount
  useEffect(() => {
    // Only fetch prompts if none exist in the store
    if (prompts.length === 0) {
      fetchSystemPrompts();
    }
  }, [prompts.length, fetchSystemPrompts]);
  
  return {
    // State
    prompts,
    defaultPromptId,
    currentPrompt,
    isLoading,
    error,
    
    // Actions
    fetchSystemPrompts,
    setConversationSystemPrompt,
    createSystemPrompt,
    updateSystemPrompt,
    deleteSystemPrompt,
    setDefaultSystemPrompt,
    getSystemPromptById,
    importSystemPrompts,
    exportSystemPrompts,
    clearError: () => setError(null)
  };
}