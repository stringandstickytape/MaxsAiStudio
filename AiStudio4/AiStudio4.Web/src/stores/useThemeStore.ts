// AiStudio4.Web/src/stores/useThemeStore.ts

import { create } from 'zustand';
import { Theme } from '@/types/theme';

interface ThemeState {
  // State
  themes: Theme[];
  currentTheme: Theme | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  setThemes: (themes: Theme[]) => void;
  setCurrentTheme: (theme: Theme | null) => void;
  addTheme: (theme: Theme) => void;
  updateTheme: (theme: Theme) => void;
  removeTheme: (themeId: string) => void;
  setLoading: (isLoading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useThemeStore = create<ThemeState>((set) => ({
  // Initial state
  themes: [],
  currentTheme: null,
  isLoading: false,
  error: null,

  // Actions
  setThemes: (themes) => set({ themes }),
  setCurrentTheme: (theme) => set({ currentTheme: theme }),
  addTheme: (theme) => set((state) => ({
    themes: [...state.themes, theme]
  })),
  updateTheme: (theme) => set((state) => ({
    themes: state.themes.map((t) => 
      t.guid === theme.guid ? theme : t
    )
  })),
  removeTheme: (themeId) => set((state) => ({
    themes: state.themes.filter((t) => t.guid !== themeId)
  })),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
}));