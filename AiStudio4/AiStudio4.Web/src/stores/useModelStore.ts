// src/stores/useModelStore.ts
import { create } from 'zustand';
import { Model, ServiceProvider } from '@/types/settings';

interface ModelStore {
  
  models: Model[];
  providers: ServiceProvider[];
  selectedPrimaryModel: string;
  selectedSecondaryModel: string;
  loading: boolean;
  error: string | null;

  
  setModels: (models: Model[]) => void;
  setProviders: (providers: ServiceProvider[]) => void;
  selectPrimaryModel: (modelName: string) => void;
  selectSecondaryModel: (modelName: string) => void;

  addModel: (model: Omit<Model, 'guid'>) => Promise<void>;
  updateModel: (model: Model) => Promise<void>;
  deleteModel: (modelGuid: string) => Promise<void>;

  addProvider: (provider: Omit<ServiceProvider, 'guid'>) => Promise<void>;
  updateProvider: (provider: ServiceProvider) => Promise<void>;
  deleteProvider: (providerGuid: string) => Promise<void>;

  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useModelStore = create<ModelStore>((set, get) => ({
  
  models: [],
  providers: [],
  selectedPrimaryModel: 'Select Model',
  selectedSecondaryModel: 'Select Model',
  loading: false,
  error: null,

  
  setModels: (models) => set({ models }),

  setProviders: (providers) => set({ providers }),

  selectPrimaryModel: (modelName) => set({ selectedPrimaryModel: modelName }),

  selectSecondaryModel: (modelName) => set({ selectedSecondaryModel: modelName }),

  addModel: async (modelData) => {
    const { setLoading, setError, setModels, models } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/addModel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(modelData),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to add model');
      }

      
      
      const fetchResponse = await fetch('/api/getModels', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      const fetchData = await fetchResponse.json();

      if (!fetchData.success) {
        throw new Error(fetchData.error || 'Failed to fetch updated models');
      }

      setModels(fetchData.models || []);
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  updateModel: async (model) => {
    const { setLoading, setError, setModels, models } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/updateModel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(model),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to update model');
      }

      
      setModels(models.map((m) => (m.guid === model.guid ? model : m)));
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  deleteModel: async (modelGuid) => {
    const { setLoading, setError, setModels, models } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/deleteModel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ modelGuid }),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to delete model');
      }

      
      setModels(models.filter((model) => model.guid !== modelGuid));
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  addProvider: async (providerData) => {
    const { setLoading, setError, setProviders, providers } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/addServiceProvider', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(providerData),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to add service provider');
      }

      
      
      const fetchResponse = await fetch('/api/getServiceProviders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      const fetchData = await fetchResponse.json();

      if (!fetchData.success) {
        throw new Error(fetchData.error || 'Failed to fetch updated providers');
      }

      setProviders(fetchData.providers || []);
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  updateProvider: async (provider) => {
    const { setLoading, setError, setProviders, providers } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/updateServiceProvider', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(provider),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to update service provider');
      }

      
      setProviders(providers.map((p) => (p.guid === provider.guid ? provider : p)));
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  deleteProvider: async (providerGuid) => {
    const { setLoading, setError, setProviders, providers } = get();

    setLoading(true);
    setError(null);

    try {
      
      const response = await fetch('/api/deleteServiceProvider', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ providerGuid }),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to delete service provider');
      }

      
      setProviders(providers.filter((provider) => provider.guid !== providerGuid));
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      throw error; 
    } finally {
      setLoading(false);
    }
  },

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),
}));


export const debugModelStore = () => {
  const state = useModelStore.getState();
  console.group('Model Store Debug');
  console.log('Models:', state.models);
  console.log('Providers:', state.providers);
  console.log('Selected Primary Model:', state.selectedPrimaryModel);
  console.log('Selected Secondary Model:', state.selectedSecondaryModel);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};


(window as any).debugModelStore = debugModelStore;

