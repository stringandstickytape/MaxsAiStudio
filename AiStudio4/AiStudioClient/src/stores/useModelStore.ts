
import { create } from 'zustand';
import { Model, ServiceProvider } from '@/types/settings';
import { initializeModelCommands } from '@/commands/modelCommands';
import { registerModelCommands, registerProviderCommands } from '@/commands/settingsCommands';
import { useModalStore } from '@/stores/useModalStore';

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
  selectPrimaryModel: (modelGuid: string) => void;
  selectSecondaryModel: (modelGuid: string) => void;

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

  
  setModels: (models) => {
    set({ models });
    if (models.length > 0) {
      const { selectPrimaryModel, selectSecondaryModel } = useModelStore.getState();
      initializeModelCommands({
        getAvailableModels: () => models,
        selectPrimaryModel: (guid) => selectPrimaryModel(guid),
        selectSecondaryModel: (guid) => selectSecondaryModel(guid),
      });
      registerModelCommands(models, () => useModalStore.getState().openModal('models'));
    }
  },

  setProviders: (providers) => {
    set({ providers });
    if (providers.length > 0) {
      registerProviderCommands(providers, () => useModalStore.getState().openModal('providers'));
    }
  },

    selectPrimaryModel: (modelGuid) => {
    const state = get();
      // Find the model by GUID to get its name
      const model = state.models.find(m => m.guid === modelGuid);
      set({ 
          selectedPrimaryModelGuid: modelGuid,
          selectedPrimaryModel: model ? model.friendlyName : modelGuid // Fallback to using the GUID as name
      });
  },

  selectSecondaryModel: (modelGuid) => {
    const state = get();

      // Find the model by GUID to get its name
      const model = state.models.find(m => m.guid === modelGuid);
      set({ 
        selectedSecondaryModelGuid: modelGuid,
          selectedSecondaryModel: model ? model.friendlyName : modelGuid // Fallback to using the GUID as name
      });

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

