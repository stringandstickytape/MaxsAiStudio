// AiStudio4.Web/src/hooks/useThemeManagement.ts

import { useState, useCallback, useEffect } from 'react';
import { Theme } from '@/types/theme';
import { useApiCallState } from '@/utils/apiUtils';
import * as themeApi from '@/api/themeApi';
import themeManagerInstance from '@/lib/ThemeManager';
import { themeEvents } from '@/commands/themeCommands';

export function useThemeManagement() {
  const [themes, setThemes] = useState<Theme[]>([]);
  const [selectedThemeIds, setSelectedThemeIds] = useState<string[]>([]);
  const { isLoading, error, executeApiCall, clearError } = useApiCallState();

  // Fetch all themes
  const fetchThemes = useCallback(async () => {
    return executeApiCall(async () => {
      const fetchedThemes = await themeApi.fetchThemes();
      // Ensure fetchedThemes is an array
      const themesArray = Array.isArray(fetchedThemes) ? fetchedThemes : [];
      setThemes(themesArray);
      // Emit event for theme commands to update
      themeEvents.emit('themes-updated', themesArray);
      return themesArray;
    });
  }, [executeApiCall]);

  // Get a theme by ID
  const getThemeById = useCallback(
    (themeId: string) => themes.find((theme) => theme.guid === themeId),
    [themes]
  );

  // Add a new theme
  const addTheme = useCallback(
    async (theme: Theme) => {
      return executeApiCall(async () => {
        const newTheme = await themeApi.addTheme(theme);
        await fetchThemes(); // Refresh the list
        return newTheme;
      });
    },
    [executeApiCall, fetchThemes]
  );

  // Delete a theme
  const deleteTheme = useCallback(
    async (themeId: string) => {
      return executeApiCall(async () => {
        await themeApi.deleteTheme(themeId);
        // Remove from selected if it was selected
        setSelectedThemeIds((prev) => prev.filter((id) => id !== themeId));
        await fetchThemes(); // Refresh the list
        return true;
      });
    },
    [executeApiCall, fetchThemes]
  );

  // Import themes from JSON
  const importThemes = useCallback(
    async (json: string) => {
      return executeApiCall(async () => {
        const importedThemes = await themeApi.importThemes(json);
        await fetchThemes(); // Refresh the list
        return importedThemes;
      });
    },
    [executeApiCall, fetchThemes]
  );

  // Export themes as JSON
  const exportThemes = useCallback(
    async (themeIds?: string[]) => {
      return executeApiCall(async () => {
        return await themeApi.exportThemes(themeIds);
      });
    },
    [executeApiCall]
  );

  // Apply a theme
  const applyTheme = useCallback(async (theme: Theme) => {
    console.log('[ThemeManagement Debug] applyTheme called with:', theme);
    
    // Apply theme visually
    console.log('[ThemeManagement Debug] Applying theme visually with themeJson:', theme.themeJson);
    themeManagerInstance.applyTheme(theme.themeJson);
    
    // Save theme to library if it doesn't exist yet
    try {
      // Check if theme already exists in library
      console.log('[ThemeManagement Debug] Checking if theme exists in library, guid:', theme.guid);
      console.log('[ThemeManagement Debug] Current themes:', themes);
      const existingTheme = themes.find(t => t.guid === theme.guid);
      
      if (!existingTheme) {
        console.log('[ThemeManagement Debug] Theme not found in library, adding it');
        // Add theme to library
        const addedTheme = await themeApi.addTheme(theme);
        console.log('[ThemeManagement Debug] Theme added to library:', addedTheme);
        await fetchThemes(); // Refresh the list
        console.log(`[ThemeManagement Debug] Theme "${theme.name}" added to library and themes refreshed`);
      } else {
        console.log('[ThemeManagement Debug] Theme already exists in library:', existingTheme);
      }
      
      // Set as default theme in backend
      console.log('[ThemeManagement Debug] Setting theme as default, guid:', theme.guid);
      await themeApi.setDefaultTheme(theme.guid);
      console.log(`[ThemeManagement Debug] Theme "${theme.name}" set as default`);
    } catch (error) {
      console.error('[ThemeManagement Debug] Error saving theme:', error);
    }
  }, [themes, fetchThemes]);

  // Toggle theme selection
  const toggleThemeSelection = useCallback((themeId: string) => {
    setSelectedThemeIds((prev) => {
      if (prev.includes(themeId)) {
        return prev.filter((id) => id !== themeId);
      } else {
        return [...prev, themeId];
      }
    });
  }, []);

  // Clear all selections
  const clearSelectedThemes = useCallback(() => {
    setSelectedThemeIds([]);
  }, []);

  // Select all themes
  const selectAllThemes = useCallback(() => {
    setSelectedThemeIds(themes.map((theme) => theme.guid));
  }, [themes]);

  return {
    themes,
    selectedThemeIds,
    isLoading,
    error,
    fetchThemes,
    getThemeById,
    addTheme,
    deleteTheme,
    importThemes,
    exportThemes,
    applyTheme,
    toggleThemeSelection,
    clearSelectedThemes,
    selectAllThemes,
    clearError,
  };
}