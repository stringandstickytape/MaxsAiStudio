// src/hooks/useModelManagement.ts
import { useCallback, useEffect } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModelStore } from '@/stores/useModelStore';
import { Model, ServiceProvider } from '@/types/settings';
import { ModelType } from '@/types/modelTypes';
import { createResourceHook } from './useResourceFactory';

const useModelResource = createResourceHook<Model>({
  endpoints: {
    fetch: '/api/getModels',
    create: '/api/addModel',
    update: '/api/updateModel',
    delete: '/api/deleteModel',
  },
  storeActions: {
    setItems: (models) => useModelStore.getState().setModels(models),
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: (data) => data.models || [],
    transformItemResponse: (data) => data.model,
  },
});

const useProviderResource = createResourceHook<ServiceProvider>({
  endpoints: {
    fetch: '/api/getServiceProviders',
    create: '/api/addServiceProvider',
    update: '/api/updateServiceProvider',
    delete: '/api/deleteServiceProvider',
  },
  storeActions: {
    setItems: (providers) => useModelStore.getState().setProviders(providers),
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: (data) => data.providers || [],
    transformItemResponse: (data) => data.provider,
  },
});

export function useModelManagement() {
  const {
    isLoading: modelsLoading,
    error: modelsError,
    fetchItems: fetchModels,
    createItem: addModel,
    updateItem: updateModel,
    deleteItem: deleteModel,
    clearError: clearModelsError,
  } = useModelResource();

  const {
    isLoading: providersLoading,
    error: providersError,
    fetchItems: fetchProviders,
    createItem: addProvider,
    updateItem: updateProvider,
    deleteItem: deleteProvider,
    clearError: clearProvidersError,
  } = useProviderResource();

  const { executeApiCall } = useApiCallState();

  const { models, providers, selectedPrimaryModel, selectedSecondaryModel, selectPrimaryModel, selectSecondaryModel } =
    useModelStore();

  useEffect(() => {
    const initializeConfig = async () => {
      if (models.length === 0 || selectedPrimaryModel === 'Select Model' || selectedSecondaryModel === 'Select Model') {
        await fetchConfig();
      }
    };

    initializeConfig();
  }, []);

  const fetchConfig = useCallback(async () => {
    return executeApiCall(async () => {
      const getConfig = createApiRequest('/api/getConfig', 'POST');
      const data = await getConfig({});

      console.log('Config loaded:', data);

      if (data.models && data.models.length > 0 && models.length === 0) {
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
          supportsPrefill: false,
        }));

        useModelStore.getState().setModels(modelObjects);
      }

      if (data.defaultModel && data.defaultModel.length > 0) {
        console.log('Setting primary model to:', data.defaultModel);
        selectPrimaryModel(data.defaultModel);
      }

      if (data.secondaryModel && data.secondaryModel.length > 0) {
        console.log('Setting secondary model to:', data.secondaryModel);
        selectSecondaryModel(data.secondaryModel);
      }

      return data;
    });
  }, [models.length, executeApiCall, selectPrimaryModel, selectSecondaryModel]);

  const handleModelSelect = useCallback(
    async (modelType: ModelType, modelName: string) => {
      return executeApiCall(async () => {
        if (modelType === 'primary') {
          selectPrimaryModel(modelName);

          const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
          await setDefaultModelRequest({ modelName });
        } else {
          selectSecondaryModel(modelName);

          const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
          await setSecondaryModelRequest({ modelName });
        }

        return true;
      });
    },
    [selectPrimaryModel, selectSecondaryModel, executeApiCall],
  );

  const getProviderName = useCallback(
    (providerGuid: string): string => {
      const provider = providers.find((p) => p.guid === providerGuid);
      return provider ? provider.friendlyName : 'Unknown Provider';
    },
    [providers],
  );

  const isLoading = modelsLoading || providersLoading;

  const error = modelsError || providersError;

  const clearError = useCallback(() => {
    clearModelsError();
    clearProvidersError();
  }, [clearModelsError, clearProvidersError]);

  return {
    models,
    providers,
    selectedPrimaryModel,
    selectedSecondaryModel,
    isLoading,
    error,
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
    clearError,
  };
}
