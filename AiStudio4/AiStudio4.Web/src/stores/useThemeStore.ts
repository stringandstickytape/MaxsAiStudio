// AiStudio4.Web/src/stores/useThemeStore.ts

import { create } from 'zustand';
import { Theme } from '@/types/theme';
import { createApiRequest } from '@/utils/apiUtils';
import themeManagerInstance from '@/lib/ThemeManager';

interface ThemeState {
  // State
  themes: Theme[];
  currentTheme: Theme | null;
  isLoading: boolean;
  error: string | null;
  selectedThemeIds: string[];

  // API Actions
  fetchThemes: () => Promise<Theme[] | null>;
  getThemeById: (themeId: string) => Promise<Theme | null>;
  addTheme: (theme: Theme) => Promise<Theme | null>;
  deleteTheme: (themeId: string) => Promise<boolean>;
  importThemes: (json: string) => Promise<Theme[] | null>;
  exportThemes: (themeIds?: string[]) => Promise<string | null>;
  setDefaultTheme: (themeId: string) => Promise<boolean>;
  getDefaultTheme: () => Promise<Theme | null>;
  applyTheme: (theme: Theme) => Promise<void>;

  // UI Actions
  setThemes: (themes: Theme[]) => void;
  setCurrentTheme: (theme: Theme | null) => void;
  updateTheme: (theme: Theme) => void;
  setLoading: (isLoading: boolean) => void;
  setError: (error: string | null) => void;
  clearError: () => void;
  toggleThemeSelection: (themeId: string) => void;
  clearSelectedThemes: () => void;
  selectAllThemes: () => void;
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  // Initial state
  themes: [],
  currentTheme: null,
  isLoading: false,
  error: null,
  selectedThemeIds: [],

  // API Actions
  fetchThemes: async () => {
    try {
      set({ isLoading: true, error: null });
        const response = await createApiRequest<void, any>('api/themes/getAll', 'POST')();
      const themes = Array.isArray(response) ? response : [];
      set({ themes, isLoading: false });
      return themes;
    } catch (error: any) {
      set({ error: error.message || 'Failed to fetch themes', isLoading: false });
      return null;
    }
  },

  getThemeById: async (themeId: string) => {
    try {
      set({ isLoading: true, error: null });
        const theme = await createApiRequest<void, Theme>(`api/themes/getById`, 'POST')();
      set({ isLoading: false });
      return theme;
    } catch (error: any) {
      set({ error: error.message || 'Failed to get theme', isLoading: false });
      return null;
    }
  },

  addTheme: async (theme: Theme) => {
    try {
      set({ isLoading: true, error: null });
        const newTheme = await createApiRequest<Theme, Theme>('api/themes/add', 'POST')(theme);
      set(state => ({
        themes: [...state.themes, newTheme],
        isLoading: false
      }));
      return newTheme;
    } catch (error: any) {
      set({ error: error.message || 'Failed to add theme', isLoading: false });
      return null;
    }
  },

  deleteTheme: async (themeId: string) => {
    try {
      set({ isLoading: true, error: null });
        await createApiRequest<void, void>(`api/themes/delete`, 'POST')();
      set(state => ({
        themes: state.themes.filter(t => t.guid !== themeId),
        selectedThemeIds: state.selectedThemeIds.filter(id => id !== themeId),
        isLoading: false
      }));
      return true;
    } catch (error: any) {
      set({ error: error.message || 'Failed to delete theme', isLoading: false });
      return false;
    }
  },

  importThemes: async (json: string) => {
    try {
      set({ isLoading: true, error: null });
      const importedThemes = await createApiRequest<string, Theme[]>('api/themes/import', 'POST')(json);
      await get().fetchThemes(); // Refresh the list
      set({ isLoading: false });
      return importedThemes;
    } catch (error: any) {
      set({ error: error.message || 'Failed to import themes', isLoading: false });
      return null;
    }
  },

  exportThemes: async (themeIds?: string[]) => {
    try {
      set({ isLoading: true, error: null });
      const json = await createApiRequest<string[] | undefined, string>('api/themes/export', 'POST')(themeIds);
      set({ isLoading: false });
      return json;
    } catch (error: any) {
      set({ error: error.message || 'Failed to export themes', isLoading: false });
      return null;
    }
  },

  setDefaultTheme: async (themeId: string) => {
    try {
      set({ isLoading: true, error: null });
      await createApiRequest<void, void>(`api/themes/setDefault/${themeId}`, 'POST')();
      set({ isLoading: false });
      return true;
    } catch (error: any) {
      set({ error: error.message || 'Failed to set default theme', isLoading: false });
      return false;
    }
  },

  getDefaultTheme: async () => {
    try {
      set({ isLoading: true, error: null });
      const theme = await createApiRequest<void, Theme>('api/themes/default', 'GET')();
      if (theme) {
        set({ currentTheme: theme, isLoading: false });
      } else {
        set({ isLoading: false });
      }
      return theme;
    } catch (error: any) {
      set({ error: error.message || 'Failed to get default theme', isLoading: false });
      return null;
    }
  },

  applyTheme: async (theme: Theme) => {
    try {
      // Apply theme visually using ThemeManager
      themeManagerInstance.applyTheme(theme.themeJson);
      
      // Set as current theme in state
      set({ currentTheme: theme });
      
      // Save as default if it exists in the library
      const { themes } = get();
      const existingTheme = themes.find(t => t.guid === theme.guid);
      
      if (existingTheme) {
        await get().setDefaultTheme(theme.guid);
      } else {
        // Add to library if it doesn't exist
        const addedTheme = await get().addTheme(theme);
        if (addedTheme) {
          await get().setDefaultTheme(addedTheme.guid);
        }
      }
    } catch (error: any) {
      set({ error: error.message || 'Failed to apply theme' });
    }
  },

  // UI Actions
  setThemes: (themes) => set({ themes }),
  
  setCurrentTheme: (theme) => set({ currentTheme: theme }),
  
  updateTheme: (theme) => set((state) => ({
    themes: state.themes.map((t) => 
      t.guid === theme.guid ? theme : t
    )
  })),
  
  setLoading: (isLoading) => set({ isLoading }),
  
  setError: (error) => set({ error }),
  
  clearError: () => set({ error: null }),
  
  toggleThemeSelection: (themeId) => set((state) => ({
    selectedThemeIds: state.selectedThemeIds.includes(themeId)
      ? state.selectedThemeIds.filter(id => id !== themeId)
      : [...state.selectedThemeIds, themeId]
  })),
  
  clearSelectedThemes: () => set({ selectedThemeIds: [] }),
  
  selectAllThemes: () => set((state) => ({
    selectedThemeIds: state.themes.map(theme => theme.guid)
  }))
}));

// Debug helper
export const debugThemeStore = () => {
  const state = useThemeStore.getState();
  console.group('Theme Store Debug');
  console.log('Themes:', state.themes);
  console.log('Current Theme:', state.currentTheme);
  console.log('Selected Theme IDs:', state.selectedThemeIds);
  console.log('Loading:', state.isLoading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

// Expose debug function globally
(window as any).debugThemeStore = debugThemeStore;