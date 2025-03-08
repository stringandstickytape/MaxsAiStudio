// src/hooks/useModelManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useModelStore } from '@/stores/useModelStore';
import { useGetConfigQuery, useSetDefaultModelMutation, useSetSecondaryModelMutation } from '@/services/api/chatApi';
import { Model, ServiceProvider } from '@/types/settings';
import { ModelType } from '@/types/modelTypes';
import { v4 as uuidv4 } from 'uuid';

/**
 * A centralized hook for managing models and providers throughout the application.
 * Combines Zustand state management with RTK Query API calls.
 */
export function useModelManagement() {
  // Local state for error handling
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Get access to the Zustand store
  const {
    models,
    providers = [], // Provide a default empty array
    selectedPrimaryModel,
    selectedSecondaryModel,
    setModels,
    setProviders,
    selectPrimaryModel,
    selectSecondaryModel,
    addModel: zustandAddModel,
    updateModel: zustandUpdateModel,
    deleteModel: zustandDeleteModel,
    addProvider: zustandAddProvider,
    updateProvider: zustandUpdateProvider,
    deleteProvider: zustandDeleteProvider,
  } = useModelStore();

  // RTK Query hooks
  const { data: configData, isLoading: isConfigLoading } = useGetConfigQuery();
  const [setDefaultModel] = useSetDefaultModelMutation();
  const [setSecondaryModel] = useSetSecondaryModelMutation();

  // Function to fetch providers if needed
  const fetchProviders = useCallback(async () => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/getServiceProviders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': localStorage.getItem('clientId') || '',
        },
        body: JSON.stringify({}),
      });
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch service providers');
      }
      
      // Ensure we're setting an array, even if data.providers is undefined
      setProviders(data.providers || []);
    } catch (err) {
      setError(`Failed to fetch providers: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error fetching providers:', err);
    } finally {
      setIsLoading(false);
    }
  }, [setProviders]);

  // Initialize models from config
  useEffect(() => {
    if (configData && !isLoading) {
      // Set available models if they don't exist already
      if (configData.models && configData.models.length > 0 && models.length === 0) {
        // Create model objects from config data
        const modelObjects = configData.models.map(modelName => ({
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
      if (configData.defaultModel && configData.defaultModel.length > 0 &&
          selectedPrimaryModel === 'Select Model') {
        selectPrimaryModel(configData.defaultModel);
      }
      
      // Set secondary model if available and not already set
      if (configData.secondaryModel && configData.secondaryModel.length > 0 &&
          selectedSecondaryModel === 'Select Model') {
        selectSecondaryModel(configData.secondaryModel);
      }
    }
  }, [configData, models.length, selectedPrimaryModel, selectedSecondaryModel, selectPrimaryModel, selectSecondaryModel, setModels, isLoading]);

  // Select model with API synchronization
  const handleModelSelect = useCallback(async (modelType: ModelType, modelName: string) => {
    setIsLoading(true);
    setError(null);
    
    try {
      // Update local state first for immediate UI response
      if (modelType === 'primary') {
        selectPrimaryModel(modelName);
        // Also update on the server
        await setDefaultModel({ modelName }).unwrap();
      } else {
        selectSecondaryModel(modelName);
        // Also update on the server
        await setSecondaryModel({ modelName }).unwrap();
      }
    } catch (err) {
      setError(`Failed to set ${modelType} model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error(`Error setting ${modelType} model:`, err);
    } finally {
      setIsLoading(false);
    }
  }, [selectPrimaryModel, selectSecondaryModel, setDefaultModel, setSecondaryModel]);

  // Add a new model with error handling
  const addModel = useCallback(async (modelData: Omit<Model, 'guid'>) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandAddModel(modelData);
      return true;
    } catch (err) {
      setError(`Failed to add model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error adding model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandAddModel]);

  // Update an existing model with error handling
  const updateModel = useCallback(async (modelData: Model) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandUpdateModel(modelData);
      return true;
    } catch (err) {
      setError(`Failed to update model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error updating model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandUpdateModel]);

  // Delete a model with error handling
  const deleteModel = useCallback(async (modelGuid: string) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandDeleteModel(modelGuid);
      return true;
    } catch (err) {
      setError(`Failed to delete model: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting model:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandDeleteModel]);

  // Add a new provider with error handling
  const addProvider = useCallback(async (providerData: Omit<ServiceProvider, 'guid'>) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandAddProvider(providerData);
      return true;
    } catch (err) {
      setError(`Failed to add provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error adding provider:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandAddProvider]);

  // Update an existing provider with error handling
  const updateProvider = useCallback(async (providerData: ServiceProvider) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandUpdateProvider(providerData);
      return true;
    } catch (err) {
      setError(`Failed to update provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error updating provider:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandUpdateProvider]);

  // Delete a provider with error handling
  const deleteProvider = useCallback(async (providerGuid: string) => {
    setIsLoading(true);
    setError(null);
    
    try {
      await zustandDeleteProvider(providerGuid);
      return true;
    } catch (err) {
      setError(`Failed to delete provider: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting provider:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [zustandDeleteProvider]);

  // Get a provider name by GUID
  const getProviderName = useCallback((providerGuid: string): string => {
    const provider = providers.find(p => p.guid === providerGuid);
    return provider ? provider.friendlyName : 'Unknown Provider';
  }, [providers]);

  // Fetch providers on initial load if empty
  useEffect(() => {
    if (providers.length === 0 && !isLoading) {
      fetchProviders();
    }
  }, [providers.length, isLoading, fetchProviders]);

  return {
    // State
    models,
    providers,
    selectedPrimaryModel,
    selectedSecondaryModel,
    isLoading: isLoading || isConfigLoading,
    error,
    
    // Actions
    handleModelSelect,
    addModel,
    updateModel,
    deleteModel,
    addProvider,
    updateProvider,
    deleteProvider,
    getProviderName,
    fetchProviders,  // Expose this so components can trigger a refresh
    clearError: () => setError(null)
  };
}