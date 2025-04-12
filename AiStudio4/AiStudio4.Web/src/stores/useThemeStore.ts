// AiStudio4.Web/src/stores/useThemeStore.ts

import { create } from 'zustand';
import { Theme } from '@/types/theme';
import { v4 as uuidv4 } from 'uuid';
import ThemeManager from '@/lib/ThemeManager';

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
    set(state => ({
      themes: [...state.themes, newTheme],
    }));
    return newTheme.guid;
  },

  updateTheme: (themeId: string, updates: Partial<Theme>) => {
    set(state => ({
      themes: state.themes.map(theme => 
        theme.guid === themeId 
          ? { ...theme, ...updates, lastModified: new Date().toISOString() }
          : theme
      )
    }));
  },

  removeTheme: (themeId: string) => {
    set(state => ({
      themes: state.themes.filter(theme => theme.guid !== themeId),
      activeThemeId: state.activeThemeId === themeId ? null : state.activeThemeId,
    }));
  },

  setActiveTheme: (themeId: string) => {
    set({ activeThemeId: themeId });
    get().applyTheme(themeId);
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