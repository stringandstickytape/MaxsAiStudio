// AiStudio4.Web/src/hooks/useThemeManagement.ts

import { useCallback } from 'react';
import { useThemeStore } from '@/stores/useThemeStore';
import { Theme } from '@/types/theme';
import { v4 as uuidv4 } from 'uuid';

/**
 * Custom hook for managing themes in the application.
 * Provides methods to create, delete, activate, and apply themes,
 * as well as access to theme state and error handling.
 */
export function useThemeManagement() {
  const { 
    themes, 
    activeThemeId, 
    isLoading, 
    error,
    addTheme,
    updateTheme,
    removeTheme,
    setActiveTheme,
    applyTheme,
    applyRandomTheme,
    setError,
    loadThemes,
    saveTheme,
    deleteThemeFromServer,
    setActiveThemeOnServer,
    loadActiveTheme
  } = useThemeStore();

  /**
   * Creates a new theme with the provided data.
   * Generates a unique guid and timestamps if not provided.
   * @param themeData Partial theme data to create the theme.
   * @returns The ID of the newly created theme.
   */
  const createTheme = useCallback((themeData: Partial<Theme>) => {
    try {
      const now = new Date().toISOString();
      const newThemeData: Partial<Theme> = {
        ...themeData,
        guid: themeData.guid || uuidv4(),
        created: themeData.created || now,
        lastModified: themeData.lastModified || now
      };
      
      const themeId = addTheme(newThemeData);
      return themeId;
    } catch (err: any) {
      setError(err?.message || 'Failed to create theme');
      throw err;
    }
  }, [addTheme, setError]);

  /**
   * Deletes a theme by its ID.
   * @param themeId The ID of the theme to delete.
   */
  const deleteTheme = useCallback((themeId: string) => {
    try {
      removeTheme(themeId);
    } catch (err: any) {
      setError(err?.message || 'Failed to delete theme');
      throw err;
    }
  }, [removeTheme, setError]);

  /**
   * Activates a theme by its ID.
   * @param themeId The ID of the theme to activate.
   */
  const activateTheme = useCallback((themeId: string) => {
    try {
      setActiveTheme(themeId);
    } catch (err: any) {
      setError(err?.message || 'Failed to activate theme');
      throw err;
    }
  }, [setActiveTheme, setError]);

  /**
   * Applies a theme by its ID.
   * @param themeId The ID of the theme to apply.
   */
  const applyThemeById = useCallback((themeId: string) => {
    try {
      applyTheme(themeId);
    } catch (err: any) {
      setError(err?.message || 'Failed to apply theme');
      throw err;
    }
  }, [applyTheme, setError]);

  /**
   * Applies a random theme from the available themes.
   */
  const applyRandom = useCallback(() => {
    try {
      applyRandomTheme();
    } catch (err: any) {
      setError(err?.message || 'Failed to apply random theme');
      throw err;
    }
  }, [applyRandomTheme, setError]);

  /**
   * Updates a theme's name.
   * @param themeId The ID of the theme to update.
   * @param name The new name for the theme.
   */
  const updateThemeName = useCallback((themeId: string, name: string) => {
    try {
      updateTheme(themeId, { name });
    } catch (err: any) {
      setError(err?.message || 'Failed to update theme name');
      throw err;
    }
  }, [updateTheme, setError]);

  /**
   * Clears the current error state.
   */
  const clearError = useCallback(() => {
    setError(null);
  }, [setError]);

  /**
   * Refreshes the themes list from the server
   */
  const refreshThemes = useCallback(async () => {
    try {
      await loadThemes();
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to refresh themes');
      return false;
    }
  }, [loadThemes, setError]);

  /**
   * Loads the active theme from the server
   */
  const loadActive = useCallback(async () => {
    try {
      await loadActiveTheme();
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to load active theme');
      return false;
    }
  }, [loadActiveTheme, setError]);

  return {
    themes,
    activeThemeId,
    isLoading,
    error,
    createTheme,
    deleteTheme,
    activateTheme,
    applyTheme: applyThemeById,
    applyRandomTheme: applyRandom,
    updateThemeName,
    clearError,
    refreshThemes,
    loadActiveTheme: loadActive
  };
}