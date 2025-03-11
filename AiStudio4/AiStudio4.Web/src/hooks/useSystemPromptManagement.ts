// src/hooks/useSystemPromptManagement.ts
import { useCallback } from 'react';
import { useInitializeIfEmpty } from '@/utils/hookUtils';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

export function useSystemPromptManagement() {
  // Use API call state utility
  const { 
    isLoading, 
    error, 
    executeApiCall, 
    clearError 
  } = useApiCallState();
  
  // Use initialization utility directly without checking prompts length
    const isInitialized = useInitializeIfEmpty(async () => {
    // Fetch prompts directly
    await fetchSystemPrompts();
  }, []);
  
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
    return executeApiCall(async () => {
      const getSystemPrompts = createApiRequest('/api/getSystemPrompts', 'POST');
      const data = await getSystemPrompts({});
      
      setPrompts(data.prompts || []);
      return data.prompts;
    });
  }, [setPrompts]);
  
  // Set a conversation's system prompt
  const setConversationSystemPrompt = useCallback(async (params: { 
    conversationId: string; 
    promptId: string;
  }) => {
    const { conversationId, promptId } = params;
    
    // Update local state first for immediate UI feedback
    setConversationPrompt(conversationId, promptId);
    
    return executeApiCall(async () => {
      const setPromptRequest = createApiRequest('/api/setConversationSystemPrompt', 'POST');
      await setPromptRequest(params);
      
      return true;
    });
  }, [setConversationPrompt]);
  
  // Create a new system prompt
  const createSystemPrompt = useCallback(async (promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>) => {
    return executeApiCall(async () => {
      // Create full prompt object with dates
      const newPrompt = {
        ...promptData,
        guid: uuidv4(),
        createdDate: new Date().toISOString(),
        modifiedDate: new Date().toISOString()
      };
      
      const createPromptRequest = createApiRequest('/api/createSystemPrompt', 'POST');
      const data = await createPromptRequest(newPrompt);
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return data.prompt;
    });
  }, [fetchSystemPrompts]);
  
  // Update an existing system prompt
  const updateSystemPrompt = useCallback(async (promptData: SystemPrompt) => {
    return executeApiCall(async () => {
      // Update modification date
      const updatedPrompt = {
          ...promptData,
          modifiedDate: new Date().toISOString()
      };

      const updatePromptRequest = createApiRequest('/api/updateSystemPrompt', 'POST');
      const data = await updatePromptRequest(updatedPrompt);
      
      // Refresh prompts list
      await fetchSystemPrompts();

      return data.prompt;
    });
    }, [fetchSystemPrompts]);

  // Delete a system prompt
  const deleteSystemPrompt = useCallback(async (promptId: string) => {
    return executeApiCall(async () => {
      const deletePromptRequest = createApiRequest('/api/deleteSystemPrompt', 'POST');
      await deletePromptRequest({ promptId });
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    });
    }, [fetchSystemPrompts]);
  
  // Set default system prompt
  const setDefaultSystemPrompt = useCallback(async (promptId: string) => {
    // Update local state first for immediate UI response
    setDefaultPromptId(promptId);
    
    return executeApiCall(async () => {
      const setDefaultPromptRequest = createApiRequest('/api/setDefaultSystemPrompt', 'POST');
      await setDefaultPromptRequest({ promptId });
      
      return true;
    });
  }, [setDefaultPromptId]);
  
  // Get a specific prompt by ID
  const getSystemPromptById = useCallback(async (promptId: string) => {
    // First check in local state
    const localPrompt = prompts.find(p => p.guid === promptId);
    if (localPrompt) return localPrompt;
    
    return executeApiCall(async () => {
      const getPromptRequest = createApiRequest('/api/getSystemPrompt', 'POST');
      const data = await getPromptRequest({ promptId });
      
      return data.prompt;
    });
  }, [prompts]);

  // Import system prompts
  const importSystemPrompts = useCallback(async (jsonData: string) => {
    return executeApiCall(async () => {
      const importPromptsRequest = createApiRequest('/api/importSystemPrompts', 'POST');
      await importPromptsRequest({ jsonData });
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    });
  }, [fetchSystemPrompts]);

  // Export system prompts
  const exportSystemPrompts = useCallback(async () => {
    return executeApiCall(async () => {
      const exportPromptsRequest = createApiRequest('/api/exportSystemPrompts', 'POST');
      const data = await exportPromptsRequest({});
      
      return data.json;
    });
  }, []);
  
  // Initialization is now handled by useInitializeIfEmpty hook
  
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
    clearError
  };
}