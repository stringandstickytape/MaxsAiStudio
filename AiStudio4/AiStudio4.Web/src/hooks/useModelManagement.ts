// src/hooks/useModelManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useModelStore } from '@/stores/useModelStore';
import { Model, ServiceProvider } from '@/types/settings';
import { ModelType } from '@/types/modelTypes';
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/services/api/apiClient';

/**
 * A centralized hook for managing models and providers throughout the application.
 * Replaces RTK Query with direct API calls managed through Zustand state.
 */
export function useModelManagement() {
  // Local state for loading and error handling
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

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
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/getConfig', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch configuration');
      }
      
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
    } catch (err) {
      setError(`Failed to fetch config: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error fetching config:', err);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [models.length, selectedPrimaryModel, selectedSecondaryModel, selectPrimaryModel, selectSecondaryModel, setModels]);

  // Function to fetch models
  const fetchModels = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/getModels', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch models');
      }
      
      setModels(data.models || []);
      return data.models;
    } catch (err) {
      setError(`Failed to fetch models: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error fetching models:', err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [setModels]);

  // Function to fetch providers
  const fetchProviders = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/getServiceProviders', {});
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch service providers');
      }
      
      setProviders(data.providers || []);
      return data.providers;
    } catch (err) {
      setError(`Failed to fetch providers: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error fetching providers:', err);
      return [];
    } finally {
      setIsLoading(false);
    }
  }, [setProviders]);

  // Add a model
  const addModel = useCallback(async (modelData: Omit<Model, 'guid'>) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Generate a new GUID if not provided
      const modelWithGuid = {
        ...modelData,
        guid: modelData.guid || uuidv4()
      };
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/addModel', modelWithGuid);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to add model');
      }
      
      // Refresh the models list
      await fetchModels();
      
      return data.model;
    } catch (err) {
      setError(`Failed to add model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error adding model:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchModels]);

  // Update a model
  const updateModel = useCallback(async (modelData: Model) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/updateModel', modelData);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update model');
      }
      
      // Refresh the models list
      await fetchModels();
      
      return data.model;
    } catch (err) {
      setError(`Failed to update model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error updating model:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchModels]);

  // Delete a model
  const deleteModel = useCallback(async (modelGuid: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/deleteModel', { modelGuid });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to delete model');
      }
      
      // Refresh the models list
      await fetchModels();
      
      return true;
    } catch (err) {
      setError(`Failed to delete model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting model:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchModels]);

  // Add a service provider
  const addProvider = useCallback(async (providerData: Omit<ServiceProvider, 'guid'>) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Generate a new GUID if not provided
      const providerWithGuid = {
        ...providerData,
        guid: providerData.guid || uuidv4()
      };
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/addServiceProvider', providerWithGuid);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to add service provider');
      }
      
      // Refresh the providers list
      await fetchProviders();
      
      return data.provider;
    } catch (err) {
      setError(`Failed to add provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error adding provider:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchProviders]);

  // Update a service provider
  const updateProvider = useCallback(async (provider: ServiceProvider) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/updateServiceProvider', provider);
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to update service provider');
      }
      
      // Refresh the providers list
      await fetchProviders();
      
      return data.provider;
    } catch (err) {
      setError(`Failed to update provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error updating provider:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchProviders]);

  // Delete a service provider
  const deleteProvider = useCallback(async (providerGuid: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Use apiClient instead of direct fetch
      const response = await apiClient.post('/api/deleteServiceProvider', { providerGuid });
      const data = response.data;
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to delete service provider');
      }
      
      // Refresh the providers list
      await fetchProviders();
      
      return true;
    } catch (err) {
      setError(`Failed to delete provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting provider:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [fetchProviders]);

  // Select model with API synchronization
  const handleModelSelect = useCallback(async (modelType: ModelType, modelName: string) => {
    try {
      setIsLoading(true);
      setError(null);
      
      // Update local state first for immediate UI response
      if (modelType === 'primary') {
        selectPrimaryModel(modelName);
        
        // Update on the server using apiClient
        const response = await apiClient.post('/api/setDefaultModel', { modelName });
        const data = response.data;
        
        if (!data.success) {
          throw new Error(data.error || 'Failed to set default model');
        }
      } else {
        selectSecondaryModel(modelName);
        
        // Update on the server using apiClient
        const response = await apiClient.post('/api/setSecondaryModel', { modelName });
        const data = response.data;
      }
      
      return true;
    } catch (err) {
      setError(`Failed to set ${modelType} model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error(`Error setting ${modelType} model:`, err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [selectPrimaryModel, selectSecondaryModel]);

  // Get a provider name by GUID
  const getProviderName = useCallback((providerGuid: string): string => {
    const provider = providers.find(p => p.guid === providerGuid);
    return provider ? provider.friendlyName : 'Unknown Provider';
  }, [providers]);

  // Initialize data on hook mount
  useEffect(() => {
    const initialize = async () => {
      // Only fetch if data isn't already loaded
      if (models.length === 0) {
        await fetchConfig();
      }
      if (providers.length === 0) {
        await fetchProviders();
      }
    };
    
    initialize();
  }, [fetchConfig, fetchProviders, models.length, providers.length]);

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
    clearError: () => setError(null)
  };
}