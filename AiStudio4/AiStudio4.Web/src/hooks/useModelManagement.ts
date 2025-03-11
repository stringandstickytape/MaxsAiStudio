// src/hooks/useModelManagement.ts
import { useCallback } from 'react';
import { useInitialization } from '@/utils/hookUtils';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModelStore } from '@/stores/useModelStore';
import { Model, ServiceProvider } from '@/types/settings';
import { ModelType } from '@/types/modelTypes';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

/**
 * A centralized hook for managing models and providers throughout the application.
 */
export function useModelManagement() {
  // Use API call state utility
  const { 
    isLoading, 
    error, 
    executeApiCall, 
    clearError 
  } = useApiCallState();
  
  // Perform initialization with safer approach
  const isInitialized = useInitialization(async () => {
    // Always fetch config first, which also handles empty models
    await fetchConfig();
    // Then fetch providers if needed
    await fetchProviders();
  }, []);

  // Get access to the Zustand store
  const {
    models,
    providers,
    selectedPrimaryModel,
    selectedSecondaryModel,
    setModels,
    setProviders,
    selectPrimaryModel,
    selectSecondaryModel
  } = useModelStore();

  // Function to fetch configuration (models and default selections)
  const fetchConfig = useCallback(async () => {
    return executeApiCall(async () => {
      // Create API request function
      const getConfig = createApiRequest('/api/getConfig', 'POST');
      const data = await getConfig({});
      
      // Handle models if they don't exist already
      if (data.models && data.models.length > 0 && models.length === 0) {
        // Create model objects from config data
        const modelObjects = data.models.map((modelName: string) => ({
          guid: uuidv4(),
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
        
        setModels(modelObjects);
      }

      // Set primary model if available and not already set
      if (data.defaultModel && data.defaultModel.length > 0 &&
          selectedPrimaryModel === 'Select Model') {
        selectPrimaryModel(data.defaultModel);
      }
      
      // Set secondary model if available and not already set
      if (data.secondaryModel && data.secondaryModel.length > 0 &&
          selectedSecondaryModel === 'Select Model') {
        selectSecondaryModel(data.secondaryModel);
      }
      
      return data;
    });
  }, [models.length, selectedPrimaryModel, selectedSecondaryModel, selectPrimaryModel, selectSecondaryModel, setModels]);

  // Fetch models
  const fetchModels = useCallback(async () => {
    return executeApiCall(async () => {
      const getModels = createApiRequest('/api/getModels', 'POST');
      const data = await getModels({});
      
      setModels(data.models || []);
      return data.models;
    });
  }, [setModels]);

  // Fetch providers
  const fetchProviders = useCallback(async () => {
    return executeApiCall(async () => {
      const getProviders = createApiRequest('/api/getServiceProviders', 'POST');
      const data = await getProviders({});
      
      setProviders(data.providers || []);
      return data.providers;
    });
  }, [setProviders]);

  // Add a model
  const addModel = useCallback(async (modelData: Omit<Model, 'guid'>) => {
    return executeApiCall(async () => {
      // Generate a new GUID if not provided
      const modelWithGuid = {
        ...modelData,
        guid: modelData.guid || uuidv4()
      };
      
      const addModelRequest = createApiRequest('/api/addModel', 'POST');
      const data = await addModelRequest(modelWithGuid);
      
      // Refresh models list
      await fetchModels();
      
      return data.model;
    });
  }, [fetchModels]);

  // Update a model
  const updateModel = useCallback(async (modelData: Model) => {
    return executeApiCall(async () => {
      const updateModelRequest = createApiRequest('/api/updateModel', 'POST');
      const data = await updateModelRequest(modelData);
      
      // Refresh models list
      await fetchModels();
      
      return data.model;
    });
  }, [fetchModels]);

  // Delete a model
  const deleteModel = useCallback(async (modelGuid: string) => {
    return executeApiCall(async () => {
      const deleteModelRequest = createApiRequest('/api/deleteModel', 'POST');
      await deleteModelRequest({ modelGuid });
      
      // Refresh models list
      await fetchModels();
      
      return true;
    });
  }, [fetchModels]);

  // Add a service provider
  const addProvider = useCallback(async (providerData: Omit<ServiceProvider, 'guid'>) => {
    return executeApiCall(async () => {
      // Generate a new GUID if not provided
      const providerWithGuid = {
        ...providerData,
        guid: providerData.guid || uuidv4()
      };
      
      const addProviderRequest = createApiRequest('/api/addServiceProvider', 'POST');
      const data = await addProviderRequest(providerWithGuid);
      
      // Refresh providers list
      await fetchProviders();
      
      return data.provider;
    });
  }, [fetchProviders]);

  // Update a service provider
  const updateProvider = useCallback(async (provider: ServiceProvider) => {
    return executeApiCall(async () => {
      const updateProviderRequest = createApiRequest('/api/updateServiceProvider', 'POST');
      const data = await updateProviderRequest(provider);
      
      // Refresh providers list
      await fetchProviders();
      
      return data.provider;
    });
  }, [fetchProviders]);

  // Delete a service provider
  const deleteProvider = useCallback(async (providerGuid: string) => {
    return executeApiCall(async () => {
      const deleteProviderRequest = createApiRequest('/api/deleteServiceProvider', 'POST');
      await deleteProviderRequest({ providerGuid });
      
      // Refresh providers list
      await fetchProviders();
      
      return true;
    });
  }, [fetchProviders]);

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
  }, [selectPrimaryModel, selectSecondaryModel]);

  // Get a provider name by GUID
  const getProviderName = useCallback((providerGuid: string): string => {
    const provider = providers.find(p => p.guid === providerGuid);
    return provider ? provider.friendlyName : 'Unknown Provider';
  }, [providers]);

  // Initialization is now handled by useInitialization hook

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