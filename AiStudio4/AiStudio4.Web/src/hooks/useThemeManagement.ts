// AiStudio4.Web/src/hooks/useThemeManagement.ts

import { useCallback } from 'react';
import { Theme } from '@/types/theme';
import { useThemeStore } from '@/stores/useThemeStore';
import themeManagerInstance from '@/lib/ThemeManager';

export function useThemeManagement() {
  const {
    // State
    themes,
    currentTheme,
    isLoading,
    error,
    selectedThemeIds,
    
    // API Actions
    fetchThemes,
    getThemeById,
    addTheme,
    deleteTheme,
    importThemes,
    exportThemes,
    setDefaultTheme,
    getDefaultTheme,
    applyTheme,
    
    // UI Actions
    toggleThemeSelection,
    clearSelectedThemes,
    selectAllThemes,
    clearError
  } = useThemeStore();

  // Initialize theme system
  const initializeThemes = useCallback(async () => {
    // First discover themeable properties from components
    await themeManagerInstance.discoverThemes();
    
    // Then fetch themes from the server
    await fetchThemes();
    
    // Get and apply the default theme if available
    const defaultTheme = await getDefaultTheme();
    if (defaultTheme) {
      await applyTheme(defaultTheme);
    }
  }, [fetchThemes, getDefaultTheme, applyTheme]);

  // Generate LLM-compatible theme schema
  const generateThemeSchema = useCallback(() => {
    return themeManagerInstance.generateLLMToolSchema();
  }, []);

  // Apply a theme from LLM-generated flat JSON
  const applyLLMTheme = useCallback((flatThemeJson: Record<string, string>) => {
    themeManagerInstance.applyLLMTheme(flatThemeJson);
  }, []);

  return {
    // State
    themes,
    currentTheme,
    isLoading,
    error,
    selectedThemeIds,
    
    // API Actions
    fetchThemes,
    getThemeById,
    addTheme,
    deleteTheme,
    importThemes,
    exportThemes,
    setDefaultTheme,
    getDefaultTheme,
    applyTheme,
    
    // UI Actions
    toggleThemeSelection,
    clearSelectedThemes,
    selectAllThemes,
    clearError,
    
    // Additional functionality
    initializeThemes,
    generateThemeSchema,
    applyLLMTheme
  };
}