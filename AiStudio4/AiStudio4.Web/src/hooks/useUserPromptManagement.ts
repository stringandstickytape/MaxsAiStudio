// src/hooks/useUserPromptManagement.ts
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { UserPrompt } from '@/types/userPrompt';
import { createResourceHook } from './useResourceFactory';

const useUserPromptResource = createResourceHook<UserPrompt>({
  endpoints: {
    fetch: '/api/getUserPrompts',
    create: '/api/createUserPrompt',
    update: '/api/updateUserPrompt',
    delete: '/api/deleteUserPrompt'
  },
  storeActions: {
    setItems: prompts => useUserPromptStore.getState().setPrompts(prompts)
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: data => data.prompts || [],
    transformItemResponse: data => data.prompt
  }
});

export function useUserPromptManagement() {
  const {
    isLoading, error, fetchItems: fetchUserPrompts,
    createItem: createPrompt, updateItem: updatePrompt,
    deleteItem: deletePrompt, clearError
  } = useUserPromptResource();

  const { prompts, favoritePromptIds, currentPrompt, toggleFavorite } = useUserPromptStore();
  const { executeApiCall } = useApiCallState();

  const createUserPrompt = useCallback(
    promptData => createPrompt({
      ...promptData,
      createdDate: new Date().toISOString(),
      modifiedDate: new Date().toISOString()
    } as any), [createPrompt]);

  const updateUserPrompt = useCallback(
    promptData => updatePrompt({
      ...promptData,
      modifiedDate: new Date().toISOString()
    }), [updatePrompt]);

  const setFavoriteUserPrompt = useCallback(
    (promptId, isFavorite) => {
      // Update local state first
      toggleFavorite(promptId);
      // Then update server
      return executeApiCall(() =>
        createApiRequest('/api/setFavoriteUserPrompt', 'POST')({ promptId, isFavorite }).then(() => true));
    }, [toggleFavorite, executeApiCall]);

  const getUserPromptById = useCallback(
    promptId => {
      const localPrompt = prompts.find(p => p.guid === promptId);
      if (localPrompt) return Promise.resolve(localPrompt);
      return executeApiCall(() =>
        createApiRequest('/api/getUserPrompt', 'POST')({ promptId }).then(data => data.prompt));
    }, [prompts, executeApiCall]);

  const importUserPrompts = useCallback(
    jsonData => executeApiCall(async () => {
      await createApiRequest('/api/importUserPrompts', 'POST')({ jsonData });
      await fetchUserPrompts();
      return true;
    }), [fetchUserPrompts, executeApiCall]);

  const exportUserPrompts = useCallback(
    () => executeApiCall(() =>
      createApiRequest('/api/exportUserPrompts', 'POST')({}).then(data => data.json)),
    [executeApiCall]);

  return {
    prompts, favoritePromptIds, currentPrompt, isLoading, error,
    fetchUserPrompts, createUserPrompt, updateUserPrompt, 
    deleteUserPrompt: deletePrompt, setFavoriteUserPrompt,
    getUserPromptById, importUserPrompts, exportUserPrompts, clearError
  };
}