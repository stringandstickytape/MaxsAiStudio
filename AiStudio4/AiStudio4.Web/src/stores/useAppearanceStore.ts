// src/stores/useAppearanceStore.ts
import { create } from 'zustand';
import { apiClient } from '@/services/api/apiClient';

interface AppearanceState {
  // State
  fontSize: number;
  isDarkMode: boolean;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  increaseFontSize: () => void;
  decreaseFontSize: () => void;
  setFontSize: (size: number) => void;
  toggleDarkMode: () => void;
  saveAppearanceSettings: () => Promise<void>;
  loadAppearanceSettings: () => Promise<void>;
  setError: (error: string | null) => void;
}

// Constants for font size limits
const MIN_FONT_SIZE = 8;
const MAX_FONT_SIZE = 24;
const DEFAULT_FONT_SIZE = 16;
const FONT_SIZE_STEP = 1;

export const useAppearanceStore = create<AppearanceState>((set, get) => ({
  // Initial state
  fontSize: DEFAULT_FONT_SIZE,
  isDarkMode: true, // Default to dark mode based on what we see in the UI
  isLoading: false,
  error: null,
  
  // Actions
  increaseFontSize: () => set((state) => {
    const newSize = Math.min(state.fontSize + FONT_SIZE_STEP, MAX_FONT_SIZE);
    document.documentElement.style.fontSize = `${newSize}px`;
    return { fontSize: newSize };
  }),
  
  decreaseFontSize: () => set((state) => {
    const newSize = Math.max(state.fontSize - FONT_SIZE_STEP, MIN_FONT_SIZE);
    document.documentElement.style.fontSize = `${newSize}px`;
    return { fontSize: newSize };
  }),
  
  setFontSize: (size) => set((state) => {
    const newSize = Math.max(Math.min(size, MAX_FONT_SIZE), MIN_FONT_SIZE);
    document.documentElement.style.fontSize = `${newSize}px`;
    return { fontSize: newSize };
  }),
  
  toggleDarkMode: () => set((state) => ({
    isDarkMode: !state.isDarkMode
  })),
  
  saveAppearanceSettings: async () => {
    const { fontSize, isDarkMode } = get();
    set({ isLoading: true, error: null });
    
    try {
      const response = await apiClient.post('/api/saveAppearanceSettings', {
        fontSize,
        isDarkMode
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
      
      // Apply settings
      const fontSize = Number(data.fontSize) || DEFAULT_FONT_SIZE;
      set({ 
        fontSize, 
        isDarkMode: data.isDarkMode ?? true 
      });
      
      // Apply font size to the document
      document.documentElement.style.fontSize = `${fontSize}px`;
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'Unknown error loading appearance settings';
      set({ error: errMsg });
      console.error('Error loading appearance settings:', err);
      
      // Still set defaults even if there's an error
      document.documentElement.style.fontSize = `${DEFAULT_FONT_SIZE}px`;
    } finally {
      set({ isLoading: false });
    }
  },
  
  setError: (error) => set({ error })
}));

// Initialize font size on load
if (typeof window !== 'undefined') {
  // Get the current font size store
  const { fontSize, loadAppearanceSettings } = useAppearanceStore.getState();
  
  // Set initial font size
  document.documentElement.style.fontSize = `${fontSize}px`;
  
  // Load settings from server
  loadAppearanceSettings().catch(err => {
    console.warn('Failed to load appearance settings:', err);
  });
}

// Export debug helper for console use
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

// Expose debug helper in window object
if (typeof window !== 'undefined') {
  (window as any).debugAppearanceStore = debugAppearanceStore;
}