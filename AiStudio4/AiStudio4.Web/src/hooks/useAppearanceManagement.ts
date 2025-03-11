import { useState, useCallback, useEffect } from 'react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

export function useAppearanceManagement() {
    const [isInitialized, setIsInitialized] = useState(false);
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

    const applySettings = useCallback(() => {
        document.documentElement.style.fontSize = `${fontSize}px`;
    }, [fontSize]);

    useEffect(() => {
        if (!isInitialized) {
            loadAppearanceSettings()
                .then(() => {
                    applySettings();
                    setIsInitialized(true);
                })
                .catch(err => {
                    console.error('Failed to load appearance settings:', err);
                    setIsInitialized(true);
                });
        }
    }, [loadAppearanceSettings, applySettings, isInitialized]);

    useEffect(() => {
        isInitialized && applySettings();
    }, [fontSize, isDarkMode, applySettings, isInitialized]);

    const saveSettings = useCallback(async () => {
        try {
            await saveAppearanceSettings();
            return true;
        } catch {
            return false;
        }
    }, [saveAppearanceSettings]);

    const resetToDefaults = useCallback(async () => {
        setFontSize(16);
        await saveSettings();
    }, [setFontSize, saveSettings]);

    return {
        fontSize,
        isDarkMode,
        isLoading,
        error,
        isInitialized,
        setFontSize,
        increaseFontSize,
        decreaseFontSize,
        toggleDarkMode,
        saveSettings,
        resetToDefaults,
        clearError: () => setError(null),
    };
}