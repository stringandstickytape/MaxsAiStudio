// AiStudioClient/src/stores/useThemeStore.ts

import { create } from 'zustand';
import { Theme } from '@/types/theme';

interface ThemeState {
  // State
  themes: Theme[];
  activeThemeId: string | null;
  loading: boolean;
  error: string | null;

  // Actions
  setThemes: (themes: Theme[]) => void;
  setActiveThemeId: (themeId: string | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  // Initial state
  themes: [],
  activeThemeId: null,
  loading: false,
  error: null,

  // Actions
  setThemes: (themes) => set({ themes }),
  setActiveThemeId: (themeId) => {
    set({ activeThemeId: themeId });
    // Optionally: persist, apply, or broadcast theme change here
  },
  setLoading: (loading) => set({ loading }),
  setError: (error) => set({ error }),
}));

// Helper functions for external use
export const debugThemeStore = () => {
  const state = useThemeStore.getState();
  console.group('Theme Store Debug');
  console.log('Themes:', state.themes);
  console.log('Active Theme ID:', state.activeThemeId);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

// Add these functions that are used in main.tsx
export const applyRandomTheme = () => {
  // This would normally call the hook function, but we'll just log for now
  return null;
};

export const addThemeToStore = (themeData: Partial<any>) => {
  return null;
};

// Expose functions to window
if (typeof window !== 'undefined') {
  (window as any).debugThemeStore = debugThemeStore;
  (window as any).applyRandomTheme = applyRandomTheme;
  (window as any).addThemeToStore = addThemeToStore;
}
