
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

