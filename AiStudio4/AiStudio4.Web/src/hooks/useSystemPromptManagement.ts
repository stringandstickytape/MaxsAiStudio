// src/hooks/useSystemPromptManagement.ts
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { createResourceHook } from './useResourceFactory';

// Create the base resource hook for system prompts
const useSystemPromptResource = createResourceHook<SystemPrompt>({
  endpoints: {
    fetch: '/api/getSystemPrompts',
    create: '/api/createSystemPrompt',
    update: '/api/updateSystemPrompt',
    delete: '/api/deleteSystemPrompt'
  },
  storeActions: {
    setItems: (prompts) => useSystemPromptStore.getState().setPrompts(prompts)
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: (data) => data.prompts || [],
    transformItemResponse: (data) => data.prompt
  }
});

export function useSystemPromptManagement() {
  // Use the base resource hook
  const {
    isLoading,
    error,
    fetchItems: fetchSystemPrompts,
    createItem: createPrompt,
    updateItem: updatePrompt,
    deleteItem: deletePrompt,
    clearError
  } = useSystemPromptResource();
  
  // Get Zustand store actions and state
  const { 
    prompts, 
    defaultPromptId,
    currentPrompt,
    setConversationPrompt,
    setDefaultPromptId,
    setCurrentPrompt
  } = useSystemPromptStore();
  
  // Use API call state for specialized operations
  const { executeApiCall } = useApiCallState();
  
  // Create a system prompt with proper timestamps
  const createSystemPrompt = useCallback(async (promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>) => {
    // Add timestamps to the prompt data
    const promptWithDates = {
      ...promptData,
      createdDate: new Date().toISOString(),
      modifiedDate: new Date().toISOString()
    };
    
    // Use the base create function
    return createPrompt(promptWithDates as any);
  }, [createPrompt]);
  
  // Update a system prompt with updated timestamp
  const updateSystemPrompt = useCallback(async (promptData: SystemPrompt) => {
    // Update the modification date
    const updatedPrompt = {
      ...promptData,
      modifiedDate: new Date().toISOString()
    };
    
    // Use the base update function
    return updatePrompt(updatedPrompt);
  }, [updatePrompt]);
  
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
  }, [setConversationPrompt, executeApiCall]);
  
  // Set default system prompt
  const setDefaultSystemPrompt = useCallback(async (promptId: string) => {
    // Update local state first for immediate UI response
    setDefaultPromptId(promptId);
    
    return executeApiCall(async () => {
      const setDefaultPromptRequest = createApiRequest('/api/setDefaultSystemPrompt', 'POST');
      await setDefaultPromptRequest({ promptId });
      
      return true;
    });
  }, [setDefaultPromptId, executeApiCall]);
  
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
  }, [prompts, executeApiCall]);

  // Import system prompts
  const importSystemPrompts = useCallback(async (jsonData: string) => {
    return executeApiCall(async () => {
      const importPromptsRequest = createApiRequest('/api/importSystemPrompts', 'POST');
      await importPromptsRequest({ jsonData });
      
      // Refresh prompts list
      await fetchSystemPrompts();
      
      return true;
    });
  }, [fetchSystemPrompts, executeApiCall]);

  // Export system prompts
  const exportSystemPrompts = useCallback(async () => {
    return executeApiCall(async () => {
      const exportPromptsRequest = createApiRequest('/api/exportSystemPrompts', 'POST');
      const data = await exportPromptsRequest({});
      
      return data.json;
    });
  }, [executeApiCall]);
  
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
    deleteSystemPrompt: deletePrompt,
    setDefaultSystemPrompt,
    getSystemPromptById,
    importSystemPrompts,
    exportSystemPrompts,
    clearError
  };
}
