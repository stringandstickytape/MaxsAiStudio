import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { UserPrompt } from '@/types/userPrompt';
import { createResourceHook } from './useResourceFactory';

export function useUserPromptManagement() {
  // Use the existing resource hook with minimal configuration
  const userPromptResource = createResourceHook<UserPrompt>({
    endpoints: {
      fetch: '/api/getUserPrompts',
      create: '/api/createUserPrompt',
      update: '/api/updateUserPrompt',
      delete: '/api/deleteUserPrompt',
      getById: '/api/getUserPrompt'
    },
    storeActions: {
      setItems: prompts => useUserPromptStore.getState().setPrompts(prompts)
    },
    options: {
      idField: 'promptId',
      generateId: true,
      transformFetchResponse: data => data.prompts || [],
      transformItemResponse: data => data.prompt
    }
  })();

  const { prompts, favoritePromptIds, currentPrompt, toggleFavorite } = useUserPromptStore();
  const { executeApiCall } = useApiCallState();

  // Add timestamp fields to create/update operations
  const createUserPrompt = useCallback(
    promptData => userPromptResource.createItem({
      ...promptData,
      createdDate: new Date().toISOString(),
      modifiedDate: new Date().toISOString()
    } as any), [userPromptResource.createItem]);

  const updateUserPrompt = useCallback(
    promptData => userPromptResource.updateItem({
      ...promptData,
      modifiedDate: new Date().toISOString()
    }), [userPromptResource.updateItem]);

  // Special operation for favorites
  const setFavoriteUserPrompt = useCallback(
    (promptId, isFavorite) => {
      toggleFavorite(promptId);
      return executeApiCall(() =>
        createApiRequest('/api/setFavoriteUserPrompt', 'POST')({ promptId, isFavorite })
          .then(() => true));
    }, [toggleFavorite, executeApiCall]);

  // Simplify the getUserPromptById implementation
  const getUserPromptById = useCallback(
    promptId => {
      const localPrompt = prompts.find(p => p.guid === promptId);
      if (localPrompt) return Promise.resolve(localPrompt);
      return executeApiCall(() =>
        createApiRequest('/api/getUserPrompt', 'POST')({ promptId })
          .then(data => data.prompt));
    }, [prompts, executeApiCall]);

  // Import/export operations
  const importUserPrompts = useCallback(
    jsonData => executeApiCall(async () => {
      await createApiRequest('/api/importUserPrompts', 'POST')({ jsonData });
      await userPromptResource.fetchItems();
      return true;
    }), [userPromptResource.fetchItems, executeApiCall]);

  const exportUserPrompts = useCallback(
    () => executeApiCall(() =>
      createApiRequest('/api/exportUserPrompts', 'POST')({})
        .then(data => data.json)),
    [executeApiCall]);

  return {
    prompts, 
    favoritePromptIds, 
    currentPrompt, 
    isLoading: userPromptResource.isLoading, 
    error: userPromptResource.error,
    fetchUserPrompts: userPromptResource.fetchItems, 
    createUserPrompt, 
    updateUserPrompt, 
    deleteUserPrompt: userPromptResource.deleteItem, 
    setFavoriteUserPrompt,
    getUserPromptById, 
    importUserPrompts, 
    exportUserPrompts, 
    clearError: userPromptResource.clearError
  };
}