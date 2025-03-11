// src/hooks/useSystemPromptManagement.ts
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { createResourceHook } from './useResourceFactory';

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
    const {
        isLoading,
        error,
        fetchItems: fetchSystemPrompts,
        createItem: createPrompt,
        updateItem: updatePrompt,
        deleteItem: deletePrompt,
        clearError
    } = useSystemPromptResource();

    const {
        prompts,
        defaultPromptId,
        currentPrompt,
        setConvPrompt,
        setDefaultPromptId,
        setCurrentPrompt
    } = useSystemPromptStore();

    const { executeApiCall } = useApiCallState();

    const createSystemPrompt = useCallback(async (promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>) => {
        const promptWithDates = {
            ...promptData,
            createdDate: new Date().toISOString(),
            modifiedDate: new Date().toISOString()
        };

        return createPrompt(promptWithDates as any);
    }, [createPrompt]);

    const updateSystemPrompt = useCallback(async (promptData: SystemPrompt) => {
        const updatedPrompt = {
            ...promptData,
            modifiedDate: new Date().toISOString()
        };

        return updatePrompt(updatedPrompt);
    }, [updatePrompt]);

    const setConvSystemPrompt = useCallback(async (params: {
        convId: string;
        promptId: string;
    }) => {
        const { convId, promptId } = params;

        setConvPrompt(convId, promptId);

        return executeApiCall(async () => {
            const setPromptRequest = createApiRequest('/api/setConvSystemPrompt', 'POST');
            await setPromptRequest(params);

            return true;
        });
    }, [setConvPrompt, executeApiCall]);

    const setDefaultSystemPrompt = useCallback(async (promptId: string) => {
        setDefaultPromptId(promptId);

        return executeApiCall(async () => {
            const setDefaultPromptRequest = createApiRequest('/api/setDefaultSystemPrompt', 'POST');
            await setDefaultPromptRequest({ promptId });

            return true;
        });
    }, [setDefaultPromptId, executeApiCall]);

    const getSystemPromptById = useCallback(async (promptId: string) => {
        const localPrompt = prompts.find(p => p.guid === promptId);
        if (localPrompt) return localPrompt;

        return executeApiCall(async () => {
            const getPromptRequest = createApiRequest('/api/getSystemPrompt', 'POST');
            const data = await getPromptRequest({ promptId });

            return data.prompt;
        });
    }, [prompts, executeApiCall]);

    const importSystemPrompts = useCallback(async (jsonData: string) => {
        return executeApiCall(async () => {
            const importPromptsRequest = createApiRequest('/api/importSystemPrompts', 'POST');
            await importPromptsRequest({ jsonData });

            await fetchSystemPrompts();

            return true;
        });
    }, [fetchSystemPrompts, executeApiCall]);

    const exportSystemPrompts = useCallback(async () => {
        return executeApiCall(async () => {
            const exportPromptsRequest = createApiRequest('/api/exportSystemPrompts', 'POST');
            const data = await exportPromptsRequest({});

            return data.json;
        });
    }, [executeApiCall]);

    return {
        prompts,
        defaultPromptId,
        currentPrompt,
        isLoading,
        error,

        fetchSystemPrompts,
        setConvSystemPrompt,
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