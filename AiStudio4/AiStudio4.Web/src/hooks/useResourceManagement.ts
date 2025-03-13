// src/hooks/useResourceManagement.ts
import { useCallback, useEffect } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModelStore } from '@/stores/useModelStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { Model, ServiceProvider } from '@/types/settings';
import { SystemPrompt } from '@/types/systemPrompt';
import { ModelType } from '@/types/modelTypes';
import { createResourceHook } from './useResourceFactory';

const createResourceConfig = <T extends { guid: string }>(
    endpoints: { fetch: string, create: string, update: string, delete: string },
    setItems: (items: T[]) => void,
    transformOptions?: {
        transformFetchResponse?: (data: any) => T[],
        transformItemResponse?: (data: any) => T
    }
) => createResourceHook<T>({
    endpoints,
    storeActions: { setItems },
    options: {
        idField: 'promptId',
        generateId: true,
        ...transformOptions
    }
});

const useModelResource = createResourceConfig<Model>(
    {
        fetch: '/api/getModels',
        create: '/api/addModel',
        update: '/api/updateModel',
        delete: '/api/deleteModel'
    },
    models => useModelStore.getState().setModels(models),
    {
        transformFetchResponse: data => data.models || [],
        transformItemResponse: data => data.model
    }
);

const useProviderResource = createResourceConfig<ServiceProvider>(
    {
        fetch: '/api/getServiceProviders',
        create: '/api/addServiceProvider',
        update: '/api/updateServiceProvider',
        delete: '/api/deleteServiceProvider'
    },
    providers => useModelStore.getState().setProviders(providers),
    {
        transformFetchResponse: data => data.providers || [],
        transformItemResponse: data => data.provider
    }
);

const useSystemPromptResource = createResourceConfig<SystemPrompt>(
    {
        fetch: '/api/getSystemPrompts',
        create: '/api/createSystemPrompt',
        update: '/api/updateSystemPrompt',
        delete: '/api/deleteSystemPrompt'
    },
    prompts => useSystemPromptStore.getState().setPrompts(prompts),
    {
        transformFetchResponse: data => data.prompts || [],
        transformItemResponse: data => data.prompt
    }
);

export function useModelManagement() {
    const {
        isLoading: modelsLoading, error: modelsError, fetchItems: fetchModels,
        createItem: addModel, updateItem: updateModel, deleteItem: deleteModel, clearError: clearModelsError
    } = useModelResource();

    const {
        isLoading: providersLoading, error: providersError, fetchItems: fetchProviders,
        createItem: addProvider, updateItem: updateProvider, deleteItem: deleteProvider, clearError: clearProvidersError
    } = useProviderResource();

    const { executeApiCall } = useApiCallState();
    const { models, providers, selectedPrimaryModel, selectedSecondaryModel, selectPrimaryModel, selectSecondaryModel } = useModelStore();

    useEffect(() => {
        if (models.length === 0 || selectedPrimaryModel === 'Select Model' || selectedSecondaryModel === 'Select Model')
            fetchConfig();
    }, []);

    const fetchConfig = useCallback(async () =>
        executeApiCall(async () => {
            const data = await createApiRequest('/api/getConfig', 'POST')({});
            console.log('Config loaded:', data);

            if (data.models?.length > 0 && models.length === 0) {
                const modelObjects = data.models.map((modelName: string) => ({
                    guid: crypto.randomUUID(),
                    modelName,
                    friendlyName: modelName,
                    providerGuid: '',
                    userNotes: '',
                    additionalParams: '',
                    input1MTokenPrice: 0,
                    output1MTokenPrice: 0,
                    color: '#4f46e5',
                    starred: false,
                    supportsPrefill: false
                }));
                useModelStore.getState().setModels(modelObjects);
            }

            data.defaultModel?.length > 0 && (console.log('Setting primary model to:', data.defaultModel), selectPrimaryModel(data.defaultModel));
            data.secondaryModel?.length > 0 && (console.log('Setting secondary model to:', data.secondaryModel), selectSecondaryModel(data.secondaryModel));

            return data;
        }), [models.length, executeApiCall, selectPrimaryModel, selectSecondaryModel]);

    const handleModelSelect = useCallback(async (modelType: ModelType, modelName: string) =>
        executeApiCall(async () => {
            if (modelType === 'primary') {
                selectPrimaryModel(modelName);
                await createApiRequest('/api/setDefaultModel', 'POST')({ modelName });
            } else {
                selectSecondaryModel(modelName);
                await createApiRequest('/api/setSecondaryModel', 'POST')({ modelName });
            }
            return true;
        }), [selectPrimaryModel, selectSecondaryModel, executeApiCall]);

    const getProviderName = useCallback(providerGuid =>
        providers.find(p => p.guid === providerGuid)?.friendlyName || 'Unknown Provider', [providers]);

    const clearError = useCallback(() => (clearModelsError(), clearProvidersError()),
        [clearModelsError, clearProvidersError]);

    return {
        models, providers, selectedPrimaryModel, selectedSecondaryModel,
        isLoading: modelsLoading || providersLoading, error: modelsError || providersError,
        fetchConfig, fetchModels, fetchProviders, handleModelSelect,
        addModel, updateModel, deleteModel, addProvider, updateProvider,
        deleteProvider, getProviderName, clearError
    };
}

export function useSystemPromptManagement() {
    const {
        isLoading, error, fetchItems: fetchSystemPrompts,
        createItem: createPrompt, updateItem: updatePrompt,
        deleteItem: deletePrompt, clearError
    } = useSystemPromptResource();

    const { prompts, defaultPromptId, currentPrompt, setConvPrompt, setDefaultPromptId } = useSystemPromptStore();
    const { executeApiCall } = useApiCallState();

    const createSystemPrompt = useCallback(
        promptData => createPrompt({
            ...promptData,
            createdDate: new Date().toISOString(),
            modifiedDate: new Date().toISOString()
        } as any), [createPrompt]);

    const updateSystemPrompt = useCallback(
        promptData => updatePrompt({
            ...promptData,
            modifiedDate: new Date().toISOString()
        }), [updatePrompt]);

    const setConvSystemPrompt = useCallback(
        params => {
            const { convId, promptId } = params;
            setConvPrompt(convId, promptId);
            return executeApiCall(() =>
                createApiRequest('/api/setConvSystemPrompt', 'POST')(params).then(() => true));
        }, [setConvPrompt, executeApiCall]);

    const setDefaultSystemPrompt = useCallback(
        promptId => {
            setDefaultPromptId(promptId);
            return executeApiCall(() =>
                createApiRequest('/api/setDefaultSystemPrompt', 'POST')({ promptId }).then(() => true));
        }, [setDefaultPromptId, executeApiCall]);

    const getSystemPromptById = useCallback(
        promptId => {
            const localPrompt = prompts.find(p => p.guid === promptId);
            if (localPrompt) return localPrompt;
            return executeApiCall(() =>
                createApiRequest('/api/getSystemPrompt', 'POST')({ promptId }).then(data => data.prompt));
        }, [prompts, executeApiCall]);

    const importSystemPrompts = useCallback(
        jsonData => executeApiCall(async () => {
            await createApiRequest('/api/importSystemPrompts', 'POST')({ jsonData });
            await fetchSystemPrompts();
            return true;
        }), [fetchSystemPrompts, executeApiCall]);

    const exportSystemPrompts = useCallback(
        () => executeApiCall(() =>
            createApiRequest('/api/exportSystemPrompts', 'POST')({}).then(data => data.json)),
        [executeApiCall]);

    return {
        prompts, defaultPromptId, currentPrompt, isLoading, error,
        fetchSystemPrompts, setConvSystemPrompt, createSystemPrompt,
        updateSystemPrompt, deleteSystemPrompt: deletePrompt,
        setDefaultSystemPrompt, getSystemPromptById,
        importSystemPrompts, exportSystemPrompts, clearError
    };
}