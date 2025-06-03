
import { create } from 'zustand';
import { apiClient } from '@/services/api/apiClient';

interface AppearanceState {
  
  fontSize: number;
  isDarkMode: boolean;
  chatPanelSize: number;
  inputBarPanelSize: number;
  isLoading: boolean;
  error: string | null;

  
  increaseFontSize: () => void;
  decreaseFontSize: () => void;
  setFontSize: (size: number) => void;
  toggleDarkMode: () => void;
  setChatPanelSize: (size: number) => void;
  setInputBarPanelSize: (size: number) => void;
  setPanelSizes: (chatSize: number, inputBarSize: number) => void;
  saveAppearanceSettings: () => Promise<void>;
  loadAppearanceSettings: () => Promise<void>;
  setError: (error: string | null) => void;
}


const MIN_FONT_SIZE = 8;
const MAX_FONT_SIZE = 24;
const DEFAULT_FONT_SIZE = 16;
const FONT_SIZE_STEP = 1;

export const useAppearanceStore = create<AppearanceState>((set, get) => ({
  
  fontSize: DEFAULT_FONT_SIZE,
  isDarkMode: true,
  chatPanelSize: 70,
  inputBarPanelSize: 30,
  isLoading: false,
  error: null,

  
  increaseFontSize: () =>
    set((state) => {
      const newSize = Math.min(state.fontSize + FONT_SIZE_STEP, MAX_FONT_SIZE);
      document.documentElement.style.fontSize = `${newSize}px`;
      return { fontSize: newSize };
    }),

  decreaseFontSize: () =>
    set((state) => {
      const newSize = Math.max(state.fontSize - FONT_SIZE_STEP, MIN_FONT_SIZE);
      document.documentElement.style.fontSize = `${newSize}px`;
      return { fontSize: newSize };
    }),

  setFontSize: (size) =>
    set((state) => {
      const newSize = Math.max(Math.min(size, MAX_FONT_SIZE), MIN_FONT_SIZE);
      document.documentElement.style.fontSize = `${newSize}px`;
      return { fontSize: newSize };
    }),

  toggleDarkMode: () =>
    set((state) => ({
      isDarkMode: !state.isDarkMode,
    })),

  setChatPanelSize: (size) =>
    set((state) => ({
      chatPanelSize: Math.max(20, Math.min(80, size)), // Constrain between 20-80%
    })),

  setInputBarPanelSize: (size) =>
    set((state) => ({
      inputBarPanelSize: Math.max(10, Math.min(50, size)), // Constrain between 10-50%
    })),

  setPanelSizes: (chatSize, inputBarSize) =>
    set((state) => ({
      chatPanelSize: Math.max(20, Math.min(80, chatSize)),
      inputBarPanelSize: Math.max(10, Math.min(50, inputBarSize)),
    })),

  saveAppearanceSettings: async () => {
    const { fontSize, isDarkMode, chatPanelSize, inputBarPanelSize } = get();
    set({ isLoading: true, error: null });

    try {
      const response = await apiClient.post('/api/saveAppearanceSettings', {
        fontSize,
        isDarkMode,
        chatPanelSize,
        inputBarPanelSize,
      });

      const data = response.data;

      if (!data.success) {
        throw new Error(data.error || 'Failed to save appearance settings');
      }
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error saving appearance settings';
      set({ error: errMsg });
      console.error('Error saving appearance settings:', err);
      throw err;
    } finally {
      set({ isLoading: false });
    }
  },

  loadAppearanceSettings: async () => {
    set({ isLoading: true, error: null });

    try {
      const response = await apiClient.post('/api/getAppearanceSettings', {});

      const data = response.data;

      if (!data.success) {
        throw new Error(data.error || 'Failed to load appearance settings');
      }

      const fontSize = Number(data.fontSize) || DEFAULT_FONT_SIZE;
      const chatPanelSize = Number(data.chatPanelSize) || 70;
      const inputBarPanelSize = Number(data.inputBarPanelSize) || 30;
      set({
        fontSize,
        isDarkMode: data.isDarkMode ?? true,
        chatPanelSize,
        inputBarPanelSize,
      });

      
      document.documentElement.style.fontSize = `${fontSize}px`;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error loading appearance settings';
      set({ error: errMsg });
      console.error('Error loading appearance settings:', err);

      
      document.documentElement.style.fontSize = `${DEFAULT_FONT_SIZE}px`;
    } finally {
      set({ isLoading: false });
    }
  },

  setError: (error) => set({ error }),
}));




if (typeof window !== 'undefined') {
  
  const { loadAppearanceSettings } = useAppearanceStore.getState();
  
  loadAppearanceSettings().catch((err) => {
    console.warn('Failed to load appearance settings:', err);
  });
}

export const fontSizeUtils = {
    applyFontSize: (size: number) => {
        if (typeof document !== 'undefined') {
            document.documentElement.style.fontSize = `${size}px`;
        }
    },

    
    increase: () => {
        const { increaseFontSize } = useAppearanceStore.getState();
        const result = increaseFontSize();
        return result;
    },

    
    decrease: () => {
        const { decreaseFontSize } = useAppearanceStore.getState();
        const result = decreaseFontSize();
        return result;
    },

    
    set: (size: number) => {
        const { setFontSize } = useAppearanceStore.getState();
        const result = setFontSize(size);
        return result;
    },

    
    saveSettings: async () => {
        const { saveAppearanceSettings } = useAppearanceStore.getState();
        try {
            await saveAppearanceSettings();
            return true;
        } catch (error) {
            console.error('Error saving font size settings:', error);
            return false;
        }
    }
};

export const debugAppearanceStore = () => {
  const state = useAppearanceStore.getState();
  console.group('Appearance Store Debug');
  console.log('Font Size:', state.fontSize);
  console.log('Dark Mode:', state.isDarkMode);
  console.log('Loading:', state.isLoading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};


if (typeof window !== 'undefined') {
  (window as any).debugAppearanceStore = debugAppearanceStore;
}

