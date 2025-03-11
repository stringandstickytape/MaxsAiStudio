// src/hooks/useAppearanceManagement.ts
import { useState, useCallback, useEffect } from 'react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

/**
 * Hook for managing appearance settings throughout the application
 */
export function useAppearanceManagement() {
  const [isInitialized, setIsInitialized] = useState(false);

  // Get store state and actions
  const {
    fontSize,
    isDarkMode,
    isLoading,
    error,
    setFontSize,
    increaseFontSize,
    decreaseFontSize,
    toggleDarkMode,
    saveAppearanceSettings,
    loadAppearanceSettings,
    setError,
  } = useAppearanceStore();

  // Function to apply settings to the document
  const applySettings = useCallback(() => {
    // Apply font size to the root element
    document.documentElement.style.fontSize = `${fontSize}px`;

    // We could apply dark mode here if needed
    // document.documentElement.classList.toggle('dark', isDarkMode);
  }, [fontSize, isDarkMode]);

  // Load settings on mount
  useEffect(() => {
    if (!isInitialized) {
      loadAppearanceSettings()
        .then(() => {
          applySettings();
          setIsInitialized(true);
        })
        .catch((err) => {
          console.error('Failed to load appearance settings:', err);
          setIsInitialized(true);
        });
    }
  }, [loadAppearanceSettings, applySettings, isInitialized]);

  // Apply settings whenever they change
  useEffect(() => {
    if (isInitialized) {
      applySettings();
    }
  }, [fontSize, isDarkMode, applySettings, isInitialized]);

  // Function to save settings
  const saveSettings = useCallback(async () => {
    try {
      await saveAppearanceSettings();
      return true;
    } catch (err) {
      return false;
    }
  }, [saveAppearanceSettings]);

  // Reset to defaults
  const resetToDefaults = useCallback(async () => {
    setFontSize(16);
    await saveSettings();
  }, [setFontSize, saveSettings]);

  return {
    // State
    fontSize,
    isDarkMode,
    isLoading,
    error,
    isInitialized,

    // Actions
    setFontSize,
    increaseFontSize,
    decreaseFontSize,
    toggleDarkMode,
    saveSettings,
    resetToDefaults,
    clearError: () => setError(null),
  };
}
