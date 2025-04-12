// AiStudio4.Web/src/stores/useThemeStore.ts

import { create } from 'zustand';
import { Theme } from '@/types/theme';
import { v4 as uuidv4 } from 'uuid';
import ThemeManager from '@/lib/ThemeManager';
import { apiClient } from '@/services/api/apiClient';

interface ThemeState {
  themes: Theme[];
  activeThemeId: string | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  addTheme: (theme: Partial<Theme>) => string;
  updateTheme: (themeId: string, updates: Partial<Theme>) => void;
  removeTheme: (themeId: string) => void;
  setActiveTheme: (themeId: string) => void;
  applyTheme: (themeId: string) => void;
  applyRandomTheme: () => string | undefined;
  setError: (error: string | null) => void;
  
  // Server persistence
  loadThemes: () => Promise<void>;
  saveTheme: (theme: Theme) => Promise<Theme>;
  deleteThemeFromServer: (themeId: string) => Promise<boolean>;
  setActiveThemeOnServer: (themeId: string) => Promise<boolean>;
  loadActiveTheme: () => Promise<void>;
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  themes: [],
  activeThemeId: null,
  isLoading: false,
  error: null,

  addTheme: (themeData: Partial<Theme>) => {
    const now = new Date().toISOString();
    const newTheme: Theme = {
      guid: themeData.guid || uuidv4(),
      name: themeData.name || 'Unnamed Theme',
      description: themeData.description || 'No description',
      author: themeData.author || 'Unknown',
      previewColors: themeData.previewColors || ['#000000'],
      themeJson: themeData.themeJson || {},
      created: themeData.created || now,
      lastModified: themeData.lastModified || now,
    };
    
    // Add to local state immediately
    set(state => ({
      themes: [...state.themes, newTheme],
    }));
    
    // Save to server in background
    get().saveTheme(newTheme).catch(err => {
      console.error('Error saving new theme to server:', err);
    });
    
    return newTheme.guid;
  },

  updateTheme: (themeId: string, updates: Partial<Theme>) => {
    // Update local state immediately
    let updatedTheme: Theme | null = null;
    
    set(state => {
      const themes = state.themes.map(theme => {
        if (theme.guid === themeId) {
          updatedTheme = { 
            ...theme, 
            ...updates, 
            lastModified: new Date().toISOString() 
          };
          return updatedTheme;
        }
        return theme;
      });
      return { themes };
    });
    
    // Save to server in background if we found and updated the theme
    if (updatedTheme) {
      get().saveTheme(updatedTheme).catch(err => {
        console.error('Error saving updated theme to server:', err);
      });
    }
  },

  removeTheme: (themeId: string) => {
    // Update local state immediately
    set(state => ({
      themes: state.themes.filter(theme => theme.guid !== themeId),
      activeThemeId: state.activeThemeId === themeId ? null : state.activeThemeId,
    }));
    
    // Delete from server in background
    get().deleteThemeFromServer(themeId).catch(err => {
      console.error('Error deleting theme from server:', err);
    });
  },

  setActiveTheme: (themeId: string) => {
    // Update local state immediately
    set({ activeThemeId: themeId });
    get().applyTheme(themeId);
    
    // Update on server in background
    get().setActiveThemeOnServer(themeId).catch(err => {
      console.error('Error setting active theme on server:', err);
    });
  },

  applyTheme: (themeId: string) => {
    const theme = get().themes.find(t => t.guid === themeId);
    if (theme) {
      // Use applyLLMTheme instead of applyTheme for flat theme objects
      ThemeManager.applyLLMTheme(theme.themeJson);
      set({ activeThemeId: themeId });
    } else {
      set({ error: `Theme with ID ${themeId} not found` });
    }
  },

  applyRandomTheme: () => {
    const { themes } = get();
    if (themes.length === 0) {
      set({ error: 'No themes available to apply' });
      return;
    }
    const randomIndex = Math.floor(Math.random() * themes.length);
    const randomTheme = themes[randomIndex];
    
    // Use window.applyLLMTheme instead of ThemeManager.applyTheme
    if (typeof window !== 'undefined' && window.applyLLMTheme) {
      window.applyLLMTheme(randomTheme.themeJson);
    } else {
      ThemeManager.applyTheme(randomTheme.themeJson);
    }
    
    set({ activeThemeId: randomTheme.guid });
    return randomTheme.guid;
  },

  setError: (error: string | null) => set({ error }),
  
  // Server persistence methods
  loadThemes: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/api/themes/getAll', {});
      if (response.data.success) {
        set({ themes: response.data.themes || [] });
      } else {
        throw new Error(response.data.error || 'Failed to load themes');
      }
    } catch (err: any) {
      set({ error: err?.message || 'Unknown error loading themes' });
      console.error('Error loading themes:', err);
    } finally {
      set({ isLoading: false });
    }
  },
  
  saveTheme: async (theme: Theme) => {
    set({ isLoading: true, error: null });
    try {
      // Always use the add endpoint for new themes
      // This ensures we don't try to update a theme that doesn't exist on the server
      const endpoint = '/api/themes/add';
      
      console.log('Saving theme:', { themeId: theme.guid, endpoint });
      
      const response = await apiClient.post(endpoint, theme);
      if (response.data.success) {
        const savedTheme = response.data.theme;
        
        // Update local state
        if (get().themes.some(t => t.guid === savedTheme.guid)) {
          set(state => ({
            themes: state.themes.map(t => t.guid === savedTheme.guid ? savedTheme : t)
          }));
        } else {
          set(state => ({
            themes: [...state.themes, savedTheme]
          }));
        }
        
        return savedTheme;
      } else {
        throw new Error(response.data.error || `Failed to add theme`);
      }
    } catch (err: any) {
      set({ error: err?.message || 'Unknown error saving theme' });
      console.error('Error saving theme:', err);
      throw err;
    } finally {
      set({ isLoading: false });
    }
  },
  
  deleteThemeFromServer: async (themeId: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/api/themes/delete', { themeId });
      if (response.data.success) {
        // Update local state
        set(state => ({
          themes: state.themes.filter(t => t.guid !== themeId),
          activeThemeId: state.activeThemeId === themeId ? null : state.activeThemeId,
        }));
        return true;
      } else {
        throw new Error(response.data.error || 'Failed to delete theme');
      }
    } catch (err: any) {
      set({ error: err?.message || 'Unknown error deleting theme' });
      console.error('Error deleting theme:', err);
      return false;
    } finally {
      set({ isLoading: false });
    }
  },
  
  setActiveThemeOnServer: async (themeId: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/api/themes/setActive', { themeId });
      if (response.data.success) {
        set({ activeThemeId: themeId });
        return true;
      } else {
        throw new Error(response.data.error || 'Failed to set active theme');
      }
    } catch (err: any) {
      set({ error: err?.message || 'Unknown error setting active theme' });
      console.error('Error setting active theme:', err);
      return false;
    } finally {
      set({ isLoading: false });
    }
  },
  
  loadActiveTheme: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/api/themes/getActive', {});
      if (response.data.success) {
        const { themeId } = response.data;
        if (themeId) {
          set({ activeThemeId: themeId });
          get().applyTheme(themeId);
        }
      } else {
        throw new Error(response.data.error || 'Failed to load active theme');
      }
    } catch (err: any) {
      set({ error: err?.message || 'Unknown error loading active theme' });
      console.error('Error loading active theme:', err);
    } finally {
      set({ isLoading: false });
    }
  },
}));

// Export functions for window access
export const applyRandomTheme = () => {
  return useThemeStore.getState().applyRandomTheme();
};

export const addThemeToStore = (themeData: Partial<Theme>) => {
  return useThemeStore.getState().addTheme(themeData);
};

// Debug helper
export const debugThemeStore = () => {
  const state = useThemeStore.getState();
  console.group('Theme Store Debug');
  console.log('Themes:', state.themes);
  console.log('Active Theme ID:', state.activeThemeId);
  console.log('Loading:', state.isLoading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

// Expose functions to window
if (typeof window !== 'undefined') {
  (window as any).debugThemeStore = debugThemeStore;
  (window as any).applyRandomTheme = applyRandomTheme;
  (window as any).addThemeToStore = addThemeToStore;
}

// Initialize by loading themes from server
if (typeof window !== 'undefined') {
  // Load themes and active theme on initialization
  const { loadThemes, loadActiveTheme } = useThemeStore.getState();
  
  // Use Promise.all to load both in parallel
  Promise.all([
    loadThemes(),
    loadActiveTheme()
  ]).catch(err => {
    console.warn('Failed to initialize theme store:', err);
  });
}