// AiStudio4/AiStudioClient/src/stores/useGeneralSettingsStore.ts
import { create } from 'zustand';
import { createApiRequest } from '@/utils/apiUtils';

interface GeneralSettingsState {
  temperature: number;
  topP: number; // Added topP

  isLoading: boolean;
  error: string | null;
  setTemperatureLocally: (temp: number) => void; // For UI responsiveness
  setTopPLocally: (topP: number) => void; // Added for UI responsiveness
  fetchSettings: () => Promise<void>;
  updateTemperatureOnServer: (temp: number) => Promise<boolean>;
}

export const useGeneralSettingsStore = create<GeneralSettingsState>((set, get) => ({
  temperature: 0.2, // Default initial value, will be overwritten by fetchSettings
  topP: 0.9, // Default initial value, will be overwritten by fetchSettings

  isLoading: false,
  error: null,

  setTemperatureLocally: (temp) => set({ temperature: temp }),
  setTopPLocally: (topP) => set({ topP: topP }), // Added setTopPLocally

  fetchSettings: async () => {
    set({ isLoading: true, error: null });
    try {
      const getConfig = createApiRequest('/api/getConfig', 'POST');
      const data = await getConfig({});
      if (data.success) {
        set({ 
          temperature: typeof data.temperature === 'number' ? data.temperature : 0.2, 
          topP: typeof data.topP === 'number' ? data.topP : 0.9, // Added topP

          isLoading: false 
        });
      } else {
        throw new Error(data.error || 'Failed to fetch settings');
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error fetching settings';
      set({ error: errorMsg, isLoading: false });
      console.error('Error fetching general settings:', err);
    }
  },

  updateTemperatureOnServer: async (temp) => {
    set({ isLoading: true, error: null });
    try {
      const setTempRequest = createApiRequest('/api/setTemperature', 'POST');
      const response = await setTempRequest({ temperature: temp });
      if (response.success) {
        set({ temperature: temp, isLoading: false }); // Confirm local state
        return true;
      } else {
        set({ error: response.error || 'Failed to update temperature on server', isLoading: false });
        // Optionally revert local state if server update fails:
        // get().fetchSettings(); 
        return false;
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error updating temperature';
      set({ error: errorMsg, isLoading: false });
      // Optionally revert local state:
      // get().fetchSettings();
      console.error('Error updating temperature on server:', err);
      return false;
    }
  },

  updateTopPOnServer: async (topP) => {
    set({ isLoading: true, error: null });
    try {
      const setTopPRequest = createApiRequest('/api/setTopP', 'POST');
      const response = await setTopPRequest({ topP: topP });
      if (response.success) {
        set({ topP: topP, isLoading: false }); // Confirm local state
        return true;
      } else {
        set({ error: response.error || 'Failed to update Top P on server', isLoading: false });
        // Optionally revert local state if server update fails:
        // get().fetchSettings(); 
        return false;
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error updating Top P';
      set({ error: errorMsg, isLoading: false });
      // Optionally revert local state:
      // get().fetchSettings();
      console.error('Error updating Top P on server:', err);
      return false;
    }
  },
}));