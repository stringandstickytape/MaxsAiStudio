
import { create } from 'zustand';
import { Model, ServiceProvider } from '@/types/settings';

interface ModelStore {
  
  models: Model[];
  providers: ServiceProvider[];
  selectedPrimaryModelGuid: string;
  selectedSecondaryModelGuid: string;
  selectedPrimaryModel: string; // Keep for backward compatibility
  selectedSecondaryModel: string; // Keep for backward compatibility
  loading: boolean;
  error: string | null;

  
  setModels: (models: Model[]) => void;
  setProviders: (providers: ServiceProvider[]) => void;
  selectPrimaryModel: (modelGuidOrName: string, isGuid?: boolean) => void;
  selectSecondaryModel: (modelGuidOrName: string, isGuid?: boolean) => void;

  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useModelStore = create<ModelStore>((set, get) => ({
  
  models: [],
  providers: [],
  selectedPrimaryModelGuid: '',
  selectedSecondaryModelGuid: '',
  selectedPrimaryModel: 'Select Model', // Keep for backward compatibility
  selectedSecondaryModel: 'Select Model', // Keep for backward compatibility
  loading: false,
  error: null,

  
  setModels: (models) => set({ models }),

  setProviders: (providers) => set({ providers }),

  selectPrimaryModel: (modelGuidOrName, isGuid = true) => {
    const state = get();
    if (isGuid) {
      // Find the model by GUID to get its name
      const model = state.models.find(m => m.guid === modelGuidOrName);
      set({ 
        selectedPrimaryModelGuid: modelGuidOrName,
        selectedPrimaryModel: model ? model.modelName : modelGuidOrName // Fallback to using the GUID as name
      });
    } else {
      // Find the model by name to get its GUID
      const model = state.models.find(m => m.modelName === modelGuidOrName);
      set({ 
        selectedPrimaryModel: modelGuidOrName,
        selectedPrimaryModelGuid: model ? model.guid : '' // Empty GUID if model not found
      });
    }
  },

  selectSecondaryModel: (modelGuidOrName, isGuid = true) => {
    const state = get();
    if (isGuid) {
      // Find the model by GUID to get its name
      const model = state.models.find(m => m.guid === modelGuidOrName);
      set({ 
        selectedSecondaryModelGuid: modelGuidOrName,
        selectedSecondaryModel: model ? model.modelName : modelGuidOrName // Fallback to using the GUID as name
      });
    } else {
      // Find the model by name to get its GUID
      const model = state.models.find(m => m.modelName === modelGuidOrName);
      set({ 
        selectedSecondaryModel: modelGuidOrName,
        selectedSecondaryModelGuid: model ? model.guid : '' // Empty GUID if model not found
      });
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

