// src/hooks/useModelManagement.ts
import { useCallback, useEffect } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModelStore } from '@/stores/useModelStore';
import { Model, ServiceProvider } from '@/types/settings';
import { ModelType } from '@/types/modelTypes';
import { createResourceHook } from './useResourceFactory';

// Create resource hook for models
const useModelResource = createResourceHook<Model>({
    endpoints: {
        fetch: '/api/getModels',
        create: '/api/addModel',
        update: '/api/updateModel',
        delete: '/api/deleteModel'
    },
    storeActions: {
        setItems: (models) => useModelStore.getState().setModels(models)
    },
    options: {
        idField: 'guid',
        generateId: true,
        transformFetchResponse: (data) => data.models || [],
        transformItemResponse: (data) => data.model
    }
});

// Create resource hook for service providers
const useProviderResource = createResourceHook<ServiceProvider>({
    endpoints: {
        fetch: '/api/getServiceProviders',
        create: '/api/addServiceProvider',
        update: '/api/updateServiceProvider',
        delete: '/api/deleteServiceProvider'
    },
    storeActions: {
        setItems: (providers) => useModelStore.getState().setProviders(providers)
    },
    options: {
        idField: 'guid',
        generateId: true,
        transformFetchResponse: (data) => data.providers || [],
        transformItemResponse: (data) => data.provider
    }
});

/**
 * A centralized hook for managing models and providers throughout the application.
 */
export function useModelManagement() {
    // Use the model resource hook
    const {
        isLoading: modelsLoading,
        error: modelsError,
        fetchItems: fetchModels,
        createItem: addModel,
        updateItem: updateModel,
        deleteItem: deleteModel,
        clearError: clearModelsError
    } = useModelResource();

    // Use the provider resource hook
    const {
        isLoading: providersLoading,
        error: providersError,
        fetchItems: fetchProviders,
        createItem: addProvider,
        updateItem: updateProvider,
        deleteItem: deleteProvider,
        clearError: clearProvidersError
    } = useProviderResource();

    // Use API call state utility for specialized operations
    const { executeApiCall } = useApiCallState();

    // Get access to the Zustand store
    const {
        models,
        providers,
        selectedPrimaryModel,
        selectedSecondaryModel,
        selectPrimaryModel,
        selectSecondaryModel
    } = useModelStore();

    // Auto-initialize on component mount
    useEffect(() => {
        const initializeConfig = async () => {
            // Only run once, if models are empty or still at default selection
            if (models.length === 0 ||
                selectedPrimaryModel === 'Select Model' ||
                selectedSecondaryModel === 'Select Model') {
                await fetchConfig();
            }
        };

        initializeConfig();
    }, []);

    // Function to fetch configuration (models and default selections)
    const fetchConfig = useCallback(async () => {
        return executeApiCall(async () => {
            // Create API request function
            const getConfig = createApiRequest('/api/getConfig', 'POST');
            const data = await getConfig({});

            console.log('Config loaded:', data);

            // Handle models if they don't exist already
            if (data.models && data.models.length > 0 && models.length === 0) {
                // Create model objects from config data
                const modelObjects = data.models.map((modelName: string) => ({
                    guid: crypto.randomUUID(),
                    modelName,
                    friendlyName: modelName,
                    providerGuid: '', // Default value, will be updated later
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

            // Explicitly set primary model directly if available
            if (data.defaultModel && data.defaultModel.length > 0) {
                console.log('Setting primary model to:', data.defaultModel);
                selectPrimaryModel(data.defaultModel);
            }

            // Explicitly set secondary model directly if available
            if (data.secondaryModel && data.secondaryModel.length > 0) {
                console.log('Setting secondary model to:', data.secondaryModel);
                selectSecondaryModel(data.secondaryModel);
            }

            return data;
        });
    }, [models.length, executeApiCall, selectPrimaryModel, selectSecondaryModel]);

    // Select model with API synchronization
    const handleModelSelect = useCallback(async (modelType: ModelType, modelName: string) => {
        return executeApiCall(async () => {
            // Update local state first for immediate UI response
            if (modelType === 'primary') {
                selectPrimaryModel(modelName);

                // Update on the server
                const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
                await setDefaultModelRequest({ modelName });
            } else {
                selectSecondaryModel(modelName);

                // Update on the server
                const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
                await setSecondaryModelRequest({ modelName });
            }

            return true;
        });
    }, [selectPrimaryModel, selectSecondaryModel, executeApiCall]);

    // Get a provider name by GUID
    const getProviderName = useCallback((providerGuid: string): string => {
        const provider = providers.find(p => p.guid === providerGuid);
        return provider ? provider.friendlyName : 'Unknown Provider';
    }, [providers]);

    // Combined loading state
    const isLoading = modelsLoading || providersLoading;

    // Combined error state
    const error = modelsError || providersError;

    // Function to clear all errors
    const clearError = useCallback(() => {
        clearModelsError();
        clearProvidersError();
    }, [clearModelsError, clearProvidersError]);

    return {
        // State
        models,
        providers,
        selectedPrimaryModel,
        selectedSecondaryModel,
        isLoading,
        error,

        // Actions
        fetchConfig,
        fetchModels,
        fetchProviders,
        handleModelSelect,
        addModel,
        updateModel,
        deleteModel,
        addProvider,
        updateProvider,
        deleteProvider,
        getProviderName,
        clearError
    };
}