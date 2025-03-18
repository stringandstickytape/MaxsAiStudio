import { useState, useCallback, useEffect } from 'react';
import { useAppearanceStore, fontSizeUtils } from '@/stores/useAppearanceStore';

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

    // Using centralized font size utility instead of local implementation
    const applySettings = useCallback(() => {
        fontSizeUtils.applyFontSize(fontSize);
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
        fontSizeUtils.set(16);
        await saveSettings();
    }, [saveSettings]);

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
