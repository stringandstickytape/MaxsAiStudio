// src/hooks/useSystemPromptManagement.ts
import { useState, useCallback, useEffect, useRef } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

export function useSystemPromptManagement() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Track initialization to prevent infinite loops
  const initialized = useRef(false);
  
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
      setError(null);
      
      const response = await apiClient.post('/api/getSystemPrompts', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch system prompts');
      }
      
      setPrompts(data.prompts || []);
      return data.prompts;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error fetching system prompts';
      setError(errMsg);
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
      setError(null);
      const { conversationId, promptId } = params;
      
      // Update local state first for immediate UI feedback
      setConversationPrompt(conversationId, promptId);
      
      // Then update on the server
      const response = await apiClient.post('/api/setConversationSystemPrompt', params);
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set conversation system prompt');
      }
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error setting conversation system prompt';
      setError(errMsg);
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
      setError(null);
      
      // Create full prompt object with dates
      const newPrompt = {
        ...promptData,
        guid: uuidv4(),
        createdDate: new Date().toISOString(),
        modifiedDate: new Date().toISOString()
      };
      
      const response = await apiClient.post('/api/createSystemPrompt', newPrompt);
      
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to create system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return data.prompt;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error creating system prompt';
      setError(errMsg);
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
      setError(null);
      
      // Update modification date
      const updatedPrompt = {
        ...promptData,
        modifiedDate: new Date().toISOString()
      };
      
      const response = await apiClient.post('/api/updateSystemPrompt', updatedPrompt);
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return data.prompt;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error updating system prompt';
      setError(errMsg);
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
      setError(null);
      
      const response = await apiClient.post('/api/deleteSystemPrompt', { promptId });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to delete system prompt');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error deleting system prompt';
      setError(errMsg);
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
      setError(null);
      
      // Update local state first for immediate UI response
      setDefaultPromptId(promptId);
      
      const response = await apiClient.post('/api/setDefaultSystemPrompt', { promptId });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to set default system prompt');
      }
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error setting default system prompt';
      setError(errMsg);
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
      setError(null);
      
      const response = await apiClient.post('/api/getSystemPrompt', { promptId });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to get system prompt');
      }
      
      return data.prompt;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error getting system prompt';
      setError(errMsg);
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
      setError(null);
      
      const response = await apiClient.post('/api/importSystemPrompts', { jsonData });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to import system prompts');
      }
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error importing system prompts';
      setError(errMsg);
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
      setError(null);
      
      const response = await apiClient.post('/api/exportSystemPrompts', {});
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to export system prompts');
      }
      
      return data.json;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error exporting system prompts';
      setError(errMsg);
      console.error('Error exporting system prompts:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);
  
  // Initialize data on hook mount - with initialization tracking
  useEffect(() => {
    // Only fetch prompts if none exist in the store and we haven't initialized yet
    if (prompts.length === 0 && !initialized.current) {
      initialized.current = true;
      fetchSystemPrompts().catch(err => {
        console.error("Failed to load system prompts:", err);
      });
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