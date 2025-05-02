// AiStudioClient/src/hooks/useThemeManagement.ts

import { useCallback } from 'react';
import { useThemeStore } from '@/stores/useThemeStore';
import { Theme } from '@/types/theme';
import { v4 as uuidv4 } from 'uuid';
import { createResourceHook } from './useResourceFactory';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import ThemeManager from '@/lib/ThemeManager';

const useThemeResource = createResourceHook<Theme>({
  endpoints: {
    fetch: '/api/themes/getAll',
    create: '/api/themes/add',
    update: '/api/themes/update',
    delete: '/api/themes/delete'
  },
  storeActions: {
    setItems: themes => useThemeStore.getState().setThemes(themes)
  },
  options: {
    idField: 'guid',
    generateId: true,
    transformFetchResponse: data => data.themes || [],
    transformItemResponse: data => data.theme
  }
});

/**
 * Custom hook for managing themes in the application.
 * Provides methods to create, delete, activate, and apply themes,
 * as well as access to theme state and error handling.
 */
export function useThemeManagement() {
  const {
    isLoading: themesLoading,
    error: themesError,
    fetchItems: fetchThemes,
    createItem: addTheme,
    updateItem: updateTheme,
    deleteItem: deleteTheme,
    clearError: clearThemesError
  } = useThemeResource();

  const { executeApiCall } = useApiCallState();
  const { themes, activeThemeId, setActiveThemeId, setThemes } = useThemeStore();

  /**
   * Refreshes the themes list from the server
   */
  const refreshThemes = useCallback(async () => {
    return await fetchThemes();
  }, [fetchThemes]);

  /**
   * Updates a theme's name.
   * @param themeId The ID of the theme to update.
   * @param name The new name for the theme.
   */
  const updateThemeName = useCallback(async (themeId: string, name: string) => {
    const theme = themes.find(t => t.guid === themeId);
    if (!theme) {
      throw new Error(`Theme with ID ${themeId} not found`);
    }
    
    return await updateTheme({
      ...theme,
      name,
      lastModified: new Date().toISOString()
    });
  }, [themes, updateTheme]);

  /**
   * Activates a theme by its ID and applies it.
   * @param themeId The ID of the theme to activate.
   */
  const activateTheme = useCallback(async (themeId: string) => {
    return executeApiCall(async () => {
      const response = await createApiRequest('/api/themes/setActive', 'POST')({ themeId });
      if (response.success) {
        setActiveThemeId(themeId);
        applyTheme(themeId);
        return true;
      } else {
        throw new Error(response.error || 'Failed to set active theme');
      }
    });
  }, [executeApiCall, setActiveThemeId]);

  /**
   * Applies a theme by its ID without setting it as active.
   * @param themeId The ID of the theme to apply.
   */
  const applyTheme = useCallback((themeId: string) => {
    const theme = themes.find(t => t.guid === themeId);
    if (theme) {
      ThemeManager.applyLLMTheme(theme.themeJson);
      return true;
    } else {
      throw new Error(`Theme with ID ${themeId} not found`);
    }
  }, [themes]);

  /**
   * Applies a random theme from the available themes.
   */
  const applyRandomTheme = useCallback(() => {
    if (themes.length === 0) {
      throw new Error('No themes available to apply');
    }
    const randomIndex = Math.floor(Math.random() * themes.length);
    const randomTheme = themes[randomIndex];
    
    ThemeManager.applyLLMTheme(randomTheme.themeJson);
    return randomTheme.guid;
  }, [themes]);

  /**
   * Loads the active theme from the server and applies it
   */
  const loadActiveTheme = useCallback(async () => {
    return executeApiCall(async () => {
      const response = await createApiRequest('/api/themes/getActive', 'POST')({});
      if (response.success) {
        const { themeId } = response;
        if (themeId) {
          setActiveThemeId(themeId);
          applyTheme(themeId);
        }
        return themeId;
      } else {
        throw new Error(response.error || 'Failed to load active theme');
      }
    });
  }, [executeApiCall, setActiveThemeId, applyTheme]);

  /**
   * Creates a new theme with the provided data.
   * @param themeData Partial theme data to create the theme.
   */
  const createTheme = useCallback(async (themeData: Partial<Theme>) => {
    const now = new Date().toISOString();
    const newThemeData: Partial<Theme> = {
      ...themeData,
      guid: themeData.guid || uuidv4(),
      created: themeData.created || now,
      lastModified: themeData.lastModified || now
    };
    
    return await addTheme(newThemeData as Theme);
  }, [addTheme]);

    /**
     * Deletes a theme by its ID.
     * @param themeId The ID of the theme to delete.
     */
    const deleteThemeById = useCallback(async (themeId: string) => {
        
        return await executeApiCall(async () => {
            const response = await createApiRequest('/api/themes/delete', 'POST')({ themeId });
            if (response.success) {
                await fetchThemes();
                return true;
            } else {
                throw new Error(response.error || 'Failed to delete theme');
            }
        });
    }, [executeApiCall, fetchThemes]);

  return {
    themes,
    activeThemeId,
    isLoading: themesLoading,
    error: themesError,
    createTheme,
    updateTheme,
    deleteTheme: deleteThemeById,
    activateTheme,
    applyTheme,
    applyRandomTheme,
    updateThemeName,
    clearError: clearThemesError,
    refreshThemes,
    loadActiveTheme
  };
}
